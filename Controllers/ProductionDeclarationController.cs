using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Molina.Bedding.Mvc.Models;
using Molina.Bedding.Mvc.Services;

namespace Molina.Bedding.Mvc.Controllers;

public class ProductionDeclarationController : Controller
{
    private const string SelectedOperatorIdsSessionKey = "ProductionDeclaration.SelectedOperatorIds";
    private const string SelectedActionIdSessionKey = "ProductionDeclaration.SelectedActionId";
    private const string SelectedLaunchOrderIdsSessionKey = "ProductionDeclaration.SelectedLaunchOrderIds";
    private const string ProductionModeSessionKey = "ProductionDeclaration.ProductionMode";
    private const string AutoFillMaxFromBarcodeSessionKey = "ProductionDeclaration.AutoFillMaxFromBarcode";
    private const string AutoInsertOnBarcodeSessionKey = "ProductionDeclaration.AutoInsertOnBarcode";
    private const string LaunchPrefillSelectionsSessionKey = "ProductionDeclaration.LaunchPrefillSelections";
    private const string DateEditAuthorizedSessionKey = "ProductionDeclaration.DateEditAuthorized";

    private static readonly IReadOnlyDictionary<string, WorkActionDefinition> WorkActionMap =
        new Dictionary<string, WorkActionDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["cassette-dichiarazione-produzione"] = new("cassette-dichiarazione-produzione", "Linea Kassetten", "Dichiarazione produzione", WorkFlowType.ProductionLaunches, "BED-RC"),
            ["trapunte-dichiarazione-produzione"] = new("trapunte-dichiarazione-produzione", "Linea Trapunte", "Dichiarazione produzione", WorkFlowType.ProductionLaunches, "BED-RT"),
            ["guanciali-dichiarazione-produzione"] = new("guanciali-dichiarazione-produzione", "Linea Guanciali", "Dichiarazione produzione", WorkFlowType.ProductionLaunches, "BED-G"),
            ["cassette-pulizia"] = new("cassette-pulizia", "Linea Kassetten", "Pulizia", WorkFlowType.Screen4Direct, null),
            ["guanciali-pulizia"] = new("guanciali-pulizia", "Linea Guanciali", "Pulizia", WorkFlowType.Screen4Direct, null),
            ["pavimento-pulizia"] = new("pavimento-pulizia", "Pavimento", "Pulizia", WorkFlowType.Screen4Direct, null),
            ["trapunte-setup-2f"] = new("trapunte-setup-2f", "Linea Trapunte", "Setup 2F", WorkFlowType.Screen4Direct, null)
        };

    private readonly IOperatorCatalogService _operatorCatalogService;
    private readonly IWorkMenuService _workMenuService;
    private readonly IProductionLaunchService _productionLaunchService;
    private readonly IProductionDeclarationPersistenceService _productionDeclarationPersistenceService;
    private readonly IDeclarationDateAuthorizationService _declarationDateAuthorizationService;

    public ProductionDeclarationController(
        IOperatorCatalogService operatorCatalogService,
        IWorkMenuService workMenuService,
        IProductionLaunchService productionLaunchService,
        IProductionDeclarationPersistenceService productionDeclarationPersistenceService,
        IDeclarationDateAuthorizationService declarationDateAuthorizationService)
    {
        _operatorCatalogService = operatorCatalogService;
        _workMenuService = workMenuService;
        _productionLaunchService = productionLaunchService;
        _productionDeclarationPersistenceService = productionDeclarationPersistenceService;
        _declarationDateAuthorizationService = declarationDateAuthorizationService;
    }

    [HttpGet]
    public IActionResult Start()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult StartOperation()
    {
        ClearCurrentFlow();
        return RedirectToAction(nameof(Operators));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Reset()
    {
        ClearCurrentFlow();
        return RedirectToAction(nameof(Start));
    }

    [HttpGet]
    public IActionResult Operators()
    {
        var selectedOperatorIds = HttpContext.Session.GetString(SelectedOperatorIdsSessionKey) ?? string.Empty;

        try
        {
            var model = new OperatorSelectionViewModel
            {
                Operators = _operatorCatalogService.GetAllActive().ToList(),
                SelectedOperatorIds = selectedOperatorIds
            };

            if (model.Operators.Count == 0)
            {
                model = model with
                {
                    ValidationMessage = "Non ci sono operatori disponibili. Controlla i dati presenti a sistema."
                };
            }

            return View(model);
        }
        catch
        {
            var model = new OperatorSelectionViewModel
            {
                Operators = [],
                SelectedOperatorIds = selectedOperatorIds,
                ValidationMessage = "Non riesco a caricare gli operatori. Controlla il collegamento al database e riprova."
            };

            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult WorkMenu(OperatorSelectionPostModel postModel)
    {
        try
        {
            var selectedIds = postModel.GetSelectedIds().ToList();
            var selectedIdsValue = string.Join(',', selectedIds);
            HttpContext.Session.SetString(SelectedOperatorIdsSessionKey, selectedIdsValue);
            HttpContext.Session.Remove(SelectedActionIdSessionKey);
            HttpContext.Session.Remove(SelectedLaunchOrderIdsSessionKey);
            HttpContext.Session.Remove(ProductionModeSessionKey);
            HttpContext.Session.Remove(AutoFillMaxFromBarcodeSessionKey);
            HttpContext.Session.Remove(AutoInsertOnBarcodeSessionKey);
            HttpContext.Session.Remove(LaunchPrefillSelectionsSessionKey);
            HttpContext.Session.Remove(DateEditAuthorizedSessionKey);

            if (selectedIds.Count == 0)
            {
                var fallbackModel = new OperatorSelectionViewModel
                {
                    Operators = _operatorCatalogService.GetAllActive().ToList(),
                    SelectedOperatorIds = selectedIdsValue,
                    ValidationMessage = "Seleziona almeno un operatore prima di proseguire."
                };

                return View("Operators", fallbackModel);
            }

            return RedirectToAction(nameof(WorkMenu));
        }
        catch
        {
            var selectedIdsValue = postModel.SelectedOperatorIds ?? string.Empty;
            HttpContext.Session.SetString(SelectedOperatorIdsSessionKey, selectedIdsValue);

            var fallbackModel = new OperatorSelectionViewModel
            {
                Operators = [],
                SelectedOperatorIds = selectedIdsValue,
                ValidationMessage = "Non riesco a leggere gli operatori dal database. Controlla il collegamento e riprova."
            };

            return View("Operators", fallbackModel);
        }
    }

    [HttpGet]
    public IActionResult WorkMenu()
    {
        var selectedOperators = LoadSelectedOperatorsFromSession();
        if (selectedOperators.Count == 0)
        {
            return RedirectToAction(nameof(Operators));
        }

        var model = new WorkMenuViewModel
        {
            SelectedOperators = selectedOperators,
            Areas = _workMenuService.GetInitialAreas().ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Launches(string actionId, string? productionMode = null)
    {
        var selectedOperators = LoadSelectedOperatorsFromSession();
        if (selectedOperators.Count == 0)
        {
            return RedirectToAction(nameof(Operators));
        }

        if (!TryGetWorkAction(actionId, out var actionDefinition) || actionDefinition.FlowType != WorkFlowType.ProductionLaunches)
        {
            return RedirectToAction(nameof(WorkMenu));
        }

        HttpContext.Session.SetString(SelectedActionIdSessionKey, actionDefinition.Id);

        var normalizedProductionMode = ResolveAndPersistProductionMode(actionDefinition, productionMode);
        if (IsTrapunteProductionAction(actionDefinition) && string.IsNullOrWhiteSpace(normalizedProductionMode))
        {
            return RedirectToAction(nameof(WorkMenu));
        }

        var selectedOrderIds = ReadSelectedLaunchOrderIds();
        var autoInsertOnBarcodeEnabled = string.Equals(HttpContext.Session.GetString(AutoInsertOnBarcodeSessionKey), "1", StringComparison.Ordinal);
        var requiresMaterialLotSelection = RequiresMaterialLotSelection(actionDefinition, normalizedProductionMode);
        var resolveMaterialLots = ShouldResolveMaterialLots(actionDefinition);

        try
        {
            var launches = _productionLaunchService.GetOpenLaunches(actionDefinition.LineCode!, resolveMaterialLots).ToList();
            ApplyProducedQuantities(actionDefinition, normalizedProductionMode, launches);
            var model = BuildLaunchesViewModel(
                actionDefinition,
                normalizedProductionMode,
                selectedOperators,
                launches,
                selectedOrderIds,
                autoInsertOnBarcodeEnabled,
                ReadLaunchPrefillSelectionsJson(),
                TempData["LaunchesValidationMessage"] as string ?? (launches.Count == 0
                    ? "Non ci sono lotti disponibili per questa linea."
                    : null),
                TempData["LaunchesSuccessMessage"] as string);

            return View(model);
        }
        catch
        {
            var model = BuildLaunchesViewModel(
                actionDefinition,
                normalizedProductionMode,
                selectedOperators,
                [],
                selectedOrderIds,
                autoInsertOnBarcodeEnabled,
                ReadLaunchPrefillSelectionsJson(),
                TempData["LaunchesValidationMessage"] as string ?? "Non riesco a caricare i lotti. Controlla il collegamento al database e riprova.",
                TempData["LaunchesSuccessMessage"] as string);

            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Screen4FromLaunches(ProductionLaunchSelectionPostModel postModel)
    {
        var selectedOperators = LoadSelectedOperatorsFromSession();
        if (selectedOperators.Count == 0)
        {
            return RedirectToAction(nameof(Operators));
        }

        if (!TryGetWorkAction(postModel.ActionId, out var actionDefinition) || actionDefinition.FlowType != WorkFlowType.ProductionLaunches)
        {
            return RedirectToAction(nameof(WorkMenu));
        }

        HttpContext.Session.SetString(SelectedActionIdSessionKey, actionDefinition.Id);

        var normalizedProductionMode = ResolveAndPersistProductionMode(actionDefinition, postModel.ProductionMode);
        if (IsTrapunteProductionAction(actionDefinition) && string.IsNullOrWhiteSpace(normalizedProductionMode))
        {
            return RedirectToAction(nameof(WorkMenu));
        }

        HttpContext.Session.SetString(AutoInsertOnBarcodeSessionKey, postModel.AutoInsertOnBarcodeEnabled ? "1" : "0");

        var selectedOrderIds = postModel.GetSelectedOrderIds().ToList();
        var resolveMaterialLots = ShouldResolveMaterialLots(actionDefinition);
        if (selectedOrderIds.Count == 0)
        {
            try
            {
                var launches = _productionLaunchService.GetOpenLaunches(actionDefinition.LineCode!, resolveMaterialLots).ToList();
                ApplyProducedQuantities(actionDefinition, normalizedProductionMode, launches);
                var model = BuildLaunchesViewModel(
                    actionDefinition,
                    normalizedProductionMode,
                    selectedOperators,
                    launches,
                    selectedOrderIds,
                    postModel.AutoInsertOnBarcodeEnabled,
                    postModel.PrefilledSelectionsJson ?? "[]",
                    "Seleziona almeno un lotto prima di proseguire.",
                    null);

                return View("Launches", model);
            }
            catch
            {
                var model = BuildLaunchesViewModel(
                    actionDefinition,
                    normalizedProductionMode,
                    selectedOperators,
                    [],
                    selectedOrderIds,
                    postModel.AutoInsertOnBarcodeEnabled,
                    postModel.PrefilledSelectionsJson ?? "[]",
                    "Non riesco a caricare i lotti. Controlla il collegamento al database e riprova.",
                    null);

                return View("Launches", model);
            }
        }

        try
        {
            var selectedLaunches = _productionLaunchService.GetOpenLaunchesByOrderIds(actionDefinition.LineCode!, selectedOrderIds, resolveMaterialLots).ToList();
            ApplyProducedQuantities(actionDefinition, normalizedProductionMode, selectedLaunches);
            var blockingMaterialLotMessage = GetBlockingMaterialLotMessage(selectedLaunches, selectedOrderIds);
            if (!string.IsNullOrWhiteSpace(blockingMaterialLotMessage))
            {
                var launches = _productionLaunchService.GetOpenLaunches(actionDefinition.LineCode!, resolveMaterialLots).ToList();
                ApplyProducedQuantities(actionDefinition, normalizedProductionMode, launches);
                var model = BuildLaunchesViewModel(
                    actionDefinition,
                    normalizedProductionMode,
                    selectedOperators,
                    launches,
                    selectedOrderIds,
                    postModel.AutoInsertOnBarcodeEnabled,
                    postModel.PrefilledSelectionsJson ?? "[]",
                    blockingMaterialLotMessage,
                    null);

                return View("Launches", model);
            }
        }
        catch
        {
            var model = BuildLaunchesViewModel(
                actionDefinition,
                normalizedProductionMode,
                selectedOperators,
                [],
                selectedOrderIds,
                postModel.AutoInsertOnBarcodeEnabled,
                postModel.PrefilledSelectionsJson ?? "[]",
                "Non riesco a validare i lotti materiale per i lanci selezionati.",
                null);

            return View("Launches", model);
        }

        var prefilledSelections = postModel.GetPrefilledSelections()
            .Where(item => selectedOrderIds.Contains(item.OrderId))
            .Select(item =>
            {
                item.SelectedMaterialLotCode = NormalizeMaterialLotCode(item.SelectedMaterialLotCode);
                return item;
            })
            .ToList();

        HttpContext.Session.SetString(SelectedLaunchOrderIdsSessionKey, string.Join(',', selectedOrderIds));
        HttpContext.Session.SetString(AutoFillMaxFromBarcodeSessionKey, postModel.AutoFillMaxOnBarcode ? "1" : "0");
        HttpContext.Session.SetString(LaunchPrefillSelectionsSessionKey, SerializeLaunchPrefillSelections(prefilledSelections));
        return RedirectToAction(nameof(Screen4));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DirectInsertFromLaunches(ProductionLaunchDirectInsertPostModel postModel)
    {
        var selectedOperators = LoadSelectedOperatorsFromSession();
        if (selectedOperators.Count == 0)
        {
            return RedirectToAction(nameof(Operators));
        }

        if (!TryGetWorkAction(postModel.ActionId, out var actionDefinition) || actionDefinition.FlowType != WorkFlowType.ProductionLaunches || string.IsNullOrWhiteSpace(actionDefinition.LineCode))
        {
            return RedirectToAction(nameof(WorkMenu));
        }

        HttpContext.Session.SetString(SelectedActionIdSessionKey, actionDefinition.Id);
        HttpContext.Session.SetString(AutoInsertOnBarcodeSessionKey, postModel.AutoInsertOnBarcodeEnabled ? "1" : "0");

        var normalizedProductionMode = ResolveAndPersistProductionMode(actionDefinition, postModel.ProductionMode);
        if (IsTrapunteProductionAction(actionDefinition) && string.IsNullOrWhiteSpace(normalizedProductionMode))
        {
            return RedirectToAction(nameof(WorkMenu));
        }

        var declaredQuantity = postModel.GetDeclaredQuantity();
        if (!declaredQuantity.HasValue || declaredQuantity.Value <= 0)
        {
            TempData["LaunchesValidationMessage"] = "Inserisci una qta dichiarata valida prima di confermare.";
            return RedirectToAction(nameof(Launches), new { actionId = actionDefinition.Id, productionMode = normalizedProductionMode });
        }

        var timingMinutesPerOperator = postModel.GetTimingMinutesPerOperator();
        if (timingMinutesPerOperator <= 0)
        {
            TempData["LaunchesValidationMessage"] = "Inserisci il timing prima di confermare l'inserimento diretto.";
            return RedirectToAction(nameof(Launches), new { actionId = actionDefinition.Id, productionMode = normalizedProductionMode });
        }

        try
        {
            var resolveMaterialLots = ShouldResolveMaterialLots(actionDefinition);
            var availableLaunches = _productionLaunchService
                .GetOpenLaunchesByOrderIds(actionDefinition.LineCode!, [postModel.OrderId], resolveMaterialLots)
                .ToList();
            ApplyProducedQuantities(actionDefinition, normalizedProductionMode, availableLaunches);
            var availableLaunch = availableLaunches.FirstOrDefault();

            if (availableLaunch is null)
            {
                TempData["LaunchesValidationMessage"] = "Non riesco a rileggere il lotto selezionato.";
                return RedirectToAction(nameof(Launches), new { actionId = actionDefinition.Id, productionMode = normalizedProductionMode });
            }

            var blockingMaterialLotMessage = GetBlockingMaterialLotMessage(new[] { availableLaunch }, new[] { postModel.OrderId });
            if (!string.IsNullOrWhiteSpace(blockingMaterialLotMessage))
            {
                TempData["LaunchesValidationMessage"] = blockingMaterialLotMessage;
                return RedirectToAction(nameof(Launches), new { actionId = actionDefinition.Id, productionMode = normalizedProductionMode });
            }

            var requiresMaterialLotSelection = RequiresMaterialLotSelection(actionDefinition, normalizedProductionMode);
            var normalizedMaterialLotCode = requiresMaterialLotSelection
                ? ResolveSelectedOrAutoMaterialLotCode(postModel.SelectedMaterialLotCode, availableLaunch.AvailableMaterialLots)
                : string.Empty;
            if (requiresMaterialLotSelection)
            {
                if (string.IsNullOrWhiteSpace(normalizedMaterialLotCode))
                {
                    TempData["LaunchesValidationMessage"] = $"Seleziona un lotto valido tra quelli disponibili per il lotto {availableLaunch!.LotCode} prima di confermare.";
                    return RedirectToAction(nameof(Launches), new { actionId = actionDefinition.Id, productionMode = normalizedProductionMode });
                }
            }

            var quantityProduced = availableLaunch.QuantityProduced ?? 0m;
            var availableQuantity = Math.Max(availableLaunch.QuantityToProduce - quantityProduced, 0m);
            if (declaredQuantity.Value > availableQuantity && !postModel.ConfirmOverLimit)
            {
                TempData["LaunchesValidationMessage"] = $"La qta dichiarata per il lotto {availableLaunch.LotCode} supera la qta residua disponibile. Conferma prima l'inserimento della qta maggiore.";
                return RedirectToAction(nameof(Launches), new { actionId = actionDefinition.Id, productionMode = normalizedProductionMode });
            }

            var totalWorkedMinutes = timingMinutesPerOperator * selectedOperators.Count;
            var request = new ProductionDeclarationInsertRequest
            {
                LineCode = actionDefinition.LineCode!,
                DeclarationDate = DateTime.Today,
                TimingMinutes = totalWorkedMinutes,
                AnomalyDescription = null,
                AnomalyMinutes = 0,
                OperatorIds = selectedOperators.Select(static item => item.Id).ToList(),
                PhaseCode = GetDeclarationPhaseCode(actionDefinition, normalizedProductionMode),
                Rows = [new ProductionDeclarationInsertRowRequest
                {
                    OrderId = availableLaunch.OrderId,
                    DeclaredQuantity = declaredQuantity.Value,
                    ArticleCode = availableLaunch.ArticleCode,
                    SelectedMaterialLotCode = normalizedMaterialLotCode
                }]
            };

            _productionDeclarationPersistenceService.InsertDeclaration(request);
            HttpContext.Session.Remove(SelectedLaunchOrderIdsSessionKey);
            TempData["LaunchesSuccessMessage"] = $"Dichiarazione inserita correttamente per il lotto {availableLaunch.LotCode}.";
            return RedirectToAction(nameof(Launches), new { actionId = actionDefinition.Id, productionMode = normalizedProductionMode });
        }
        catch (Exception ex)
        {
            TempData["LaunchesValidationMessage"] = $"Non riesco a inserire la dichiarazione. {ex.Message}";
            return RedirectToAction(nameof(Launches), new { actionId = actionDefinition.Id, productionMode = normalizedProductionMode });
        }
    }

    [HttpGet]
    public IActionResult Screen4(string? actionId = null)
    {
        var selectedOperators = LoadSelectedOperatorsFromSession();
        if (selectedOperators.Count == 0)
        {
            return RedirectToAction(nameof(Operators));
        }

        var resolvedActionId = actionId;
        if (string.IsNullOrWhiteSpace(resolvedActionId))
        {
            resolvedActionId = HttpContext.Session.GetString(SelectedActionIdSessionKey);
        }

        if (string.IsNullOrWhiteSpace(resolvedActionId) || !TryGetWorkAction(resolvedActionId, out var actionDefinition))
        {
            return RedirectToAction(nameof(WorkMenu));
        }

        HttpContext.Session.SetString(SelectedActionIdSessionKey, actionDefinition.Id);

        var model = BuildScreen4Model(actionDefinition, selectedOperators);
        model.ValidationMessage = TempData["Screen4ValidationMessage"] as string;
        model.SuccessMessage = TempData["Screen4SuccessMessage"] as string;
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult AuthorizeDeclarationDateEdit([FromBody] DeclarationDateAuthorizationPostModel postModel)
    {
        var selectedOperators = LoadSelectedOperatorsFromSession();
        if (selectedOperators.Count == 0)
        {
            return Json(new { success = false, message = "Sessione operatori non valida. Riapri il flusso." });
        }

        var authorizationResult = _declarationDateAuthorizationService.AuthorizeForDateEdit(
            selectedOperators.Select(static item => item.Id),
            postModel.Pin);

        if (!authorizationResult.Success)
        {
            return Json(new { success = false, message = authorizationResult.Message });
        }

        HttpContext.Session.SetString(DateEditAuthorizedSessionKey, "1");
        return Json(new { success = true, message = authorizationResult.Message, level = authorizationResult.Level });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult InsertDeclarations(Screen4InsertPostModel postModel)
    {
        var selectedOperators = LoadSelectedOperatorsFromSession();
        if (selectedOperators.Count == 0)
        {
            return RedirectToAction(nameof(Operators));
        }

        if (!TryGetWorkAction(postModel.ActionId, out var actionDefinition))
        {
            return RedirectToAction(nameof(WorkMenu));
        }

        HttpContext.Session.SetString(SelectedActionIdSessionKey, actionDefinition.Id);
        ResolveAndPersistProductionMode(actionDefinition, postModel.ProductionMode);

        var declarationDate = postModel.GetDeclarationDateOrDefault(DateTime.Today);
        if (declarationDate.Date != DateTime.Today && !IsDateEditAuthorized(selectedOperators))
        {
            var invalidDateModel = BuildScreen4Model(actionDefinition, selectedOperators);
            ApplyPostedValues(invalidDateModel, postModel);
            invalidDateModel.ValidationMessage = "Per modificare la data devi prima inserire un PIN valido.";
            return View("Screen4", invalidDateModel);
        }

        if (actionDefinition.FlowType == WorkFlowType.Screen4Direct)
        {
            var timingMinutesPerOperatorDirect = postModel.GetTimingMinutesPerOperator();
            if (timingMinutesPerOperatorDirect <= 0)
            {
                var invalidDirectModel = BuildScreen4Model(actionDefinition, selectedOperators);
                ApplyPostedValues(invalidDirectModel, postModel);
                invalidDirectModel.ValidationMessage = "Inserisci il timing prima di premere Inserisci.";
                return View("Screen4", invalidDirectModel);
            }

            ClearCurrentFlow();
            TempData["StartSuccessMessage"] = "Operazione inserita correttamente.";
            return RedirectToAction(nameof(Start));
        }

        if (actionDefinition.FlowType != WorkFlowType.ProductionLaunches || string.IsNullOrWhiteSpace(actionDefinition.LineCode))
        {
            TempData["Screen4ValidationMessage"] = "L'inserimento è disponibile solo per la schermata con i lotti selezionati.";
            return RedirectToAction(nameof(Screen4), new { actionId = postModel.ActionId });
        }

        var declaredRows = postModel.GetDeclaredRows();
        if (declaredRows.Count == 0)
        {
            var invalidModel = BuildScreen4Model(actionDefinition, selectedOperators);
            ApplyPostedValues(invalidModel, postModel);
            invalidModel.ValidationMessage = "Inserisci almeno una qta dichiarata prima di premere Inserisci.";
            return View("Screen4", invalidModel);
        }

        var timingMinutesPerOperator = postModel.GetTimingMinutesPerOperator();
        if (timingMinutesPerOperator <= 0)
        {
            var invalidModel = BuildScreen4Model(actionDefinition, selectedOperators);
            ApplyPostedValues(invalidModel, postModel);
            invalidModel.ValidationMessage = "Inserisci il timing prima di premere Inserisci.";
            return View("Screen4", invalidModel);
        }

        var selectedMaterialLots = postModel.GetSelectedMaterialLotByOrderId();
        var confirmedOverLimitOrderIds = postModel.GetConfirmedOverLimitOrderIds();
        var requiresMaterialLotSelection = RequiresMaterialLotSelection(actionDefinition, GetSelectedProductionMode());

        var resolveMaterialLots = ShouldResolveMaterialLots(actionDefinition);
        var availableLaunchList = _productionLaunchService
            .GetOpenLaunchesByOrderIds(actionDefinition.LineCode!, declaredRows.Select(static row => row.OrderId).ToList(), resolveMaterialLots)
            .ToList();
        ApplyProducedQuantities(actionDefinition, postModel.ProductionMode, availableLaunchList);
        var availableLaunches = availableLaunchList.ToDictionary(static item => item.OrderId);
        var normalizedMaterialLotsByOrderId = new Dictionary<int, string>();

        foreach (var declaredRow in declaredRows)
        {
            if (!availableLaunches.TryGetValue(declaredRow.OrderId, out var launch))
            {
                var invalidModel = BuildScreen4Model(actionDefinition, selectedOperators);
                ApplyPostedValues(invalidModel, postModel);
                invalidModel.ValidationMessage = "Non riesco a validare uno o più lotti selezionati.";
                return View("Screen4", invalidModel);
            }

            if (!string.IsNullOrWhiteSpace(launch.MaterialLotValidationMessage))
            {
                var invalidModel = BuildScreen4Model(actionDefinition, selectedOperators);
                ApplyPostedValues(invalidModel, postModel);
                invalidModel.ValidationMessage = launch.MaterialLotValidationMessage;
                return View("Screen4", invalidModel);
            }

            selectedMaterialLots.TryGetValue(declaredRow.OrderId, out var materialLotCode);
            var normalizedMaterialLotCode = requiresMaterialLotSelection
                ? ResolveSelectedOrAutoMaterialLotCode(materialLotCode, launch.AvailableMaterialLots)
                : string.Empty;
            normalizedMaterialLotsByOrderId[declaredRow.OrderId] = normalizedMaterialLotCode;
            if (requiresMaterialLotSelection && string.IsNullOrWhiteSpace(normalizedMaterialLotCode))
            {
                var invalidModel = BuildScreen4Model(actionDefinition, selectedOperators);
                ApplyPostedValues(invalidModel, postModel);
                invalidModel.ValidationMessage = $"Seleziona un lotto valido tra quelli disponibili per il lotto {launch.LotCode} prima di inserire la qta dichiarata.";
                return View("Screen4", invalidModel);
            }

            var quantityToProduce = launch.QuantityToProduce;
            var quantityProduced = launch.QuantityProduced ?? 0m;
            var availableQuantity = Math.Max(quantityToProduce - quantityProduced, 0m);
            if (declaredRow.DeclaredQuantity > availableQuantity && !confirmedOverLimitOrderIds.Contains(declaredRow.OrderId))
            {
                var invalidModel = BuildScreen4Model(actionDefinition, selectedOperators);
                ApplyPostedValues(invalidModel, postModel);
                invalidModel.ValidationMessage = $"La qta dichiarata per il lotto {launch.LotCode} supera la qta residua disponibile. Conferma prima l'inserimento della qta maggiore.";
                return View("Screen4", invalidModel);
            }
        }

        var totalWorkedMinutes = timingMinutesPerOperator * selectedOperators.Count;

        var request = new ProductionDeclarationInsertRequest
        {
            LineCode = actionDefinition.LineCode!,
            DeclarationDate = declarationDate,
            TimingMinutes = totalWorkedMinutes,
            AnomalyDescription = string.IsNullOrWhiteSpace(postModel.GlobalProblemDescription)
                ? null
                : postModel.GlobalProblemDescription.Trim(),
            AnomalyMinutes = postModel.GetProblemMinutes(),
            OperatorIds = selectedOperators.Select(static item => item.Id).ToList(),
            PhaseCode = GetDeclarationPhaseCode(actionDefinition, postModel.ProductionMode),
            Rows = declaredRows
                .Select(row =>
                {
                    var launch = availableLaunches[row.OrderId];
                    normalizedMaterialLotsByOrderId.TryGetValue(row.OrderId, out var normalizedMaterialLotCode);
                    return new ProductionDeclarationInsertRowRequest
                    {
                        OrderId = row.OrderId,
                        DeclaredQuantity = row.DeclaredQuantity,
                        ArticleCode = launch.ArticleCode,
                        SelectedMaterialLotCode = normalizedMaterialLotCode ?? string.Empty
                    };
                })
                .ToList()
        };

        try
        {
            _productionDeclarationPersistenceService.InsertDeclaration(request);
            ClearCurrentFlow();
            TempData["StartSuccessMessage"] = "Dichiarazione inserita correttamente.";
            return RedirectToAction(nameof(Start));
        }
        catch (Exception ex)
        {
            var model = BuildScreen4Model(actionDefinition, selectedOperators);
            ApplyPostedValues(model, postModel);
            model.ValidationMessage = $"Non riesco a inserire la dichiarazione. {ex.Message}";
            return View("Screen4", model);
        }
    }

    private Screen4ViewModel BuildScreen4Model(WorkActionDefinition actionDefinition, List<OperatorItemViewModel> selectedOperators)
    {
        var isTimingOnlyMode = actionDefinition.FlowType == WorkFlowType.Screen4Direct;
        var productionMode = IsTrapunteProductionAction(actionDefinition)
            ? GetSelectedProductionMode() ?? string.Empty
            : string.Empty;
        var canRequestDateEdit = _declarationDateAuthorizationService.CanAnySelectedOperatorEditDate(selectedOperators.Select(static item => item.Id));
        var isDateEditAuthorized = IsDateEditAuthorized(selectedOperators);
        var autoFillMaxQuantityFromBarcode = string.Equals(HttpContext.Session.GetString(AutoFillMaxFromBarcodeSessionKey), "1", StringComparison.Ordinal);
        var resolveMaterialLots = ShouldResolveMaterialLots(actionDefinition);
        HttpContext.Session.Remove(AutoFillMaxFromBarcodeSessionKey);

        var model = new Screen4ViewModel
        {
            ActionId = actionDefinition.Id,
            ActionText = actionDefinition.ActionText,
            AreaTitle = actionDefinition.AreaTitle,
            LineCode = actionDefinition.LineCode,
            LineDisplayName = actionDefinition.AreaTitle,
            ProductionMode = productionMode,
            SelectedOperators = selectedOperators,
            BackActionName = actionDefinition.FlowType == WorkFlowType.ProductionLaunches ? nameof(Launches) : nameof(WorkMenu),
            IsTimingOnlyMode = isTimingOnlyMode,
            RequiresMaterialLotSelection = RequiresMaterialLotSelection(actionDefinition, productionMode),
            AvailableMaterialLots = [],
            DeclarationDate = DateTime.Today,
            CanRequestDateEdit = canRequestDateEdit,
            IsDeclarationDateEditable = isDateEditAuthorized,
            AutoFillMaxQuantityFromBarcode = autoFillMaxQuantityFromBarcode
        };

        if (actionDefinition.FlowType != WorkFlowType.ProductionLaunches)
        {
            HttpContext.Session.Remove(SelectedLaunchOrderIdsSessionKey);
            return model;
        }

        var selectedOrderIds = ReadSelectedLaunchOrderIds();
        if (selectedOrderIds.Count == 0)
        {
            return model;
        }

        try
        {
            var selectedLaunches = _productionLaunchService.GetOpenLaunchesByOrderIds(actionDefinition.LineCode!, selectedOrderIds, resolveMaterialLots).ToList();
            var historyLookup = _productionDeclarationPersistenceService
                .GetPreviousDeclarationsByOrderIds(actionDefinition.LineCode!, GetDeclarationPhaseCode(actionDefinition, productionMode), selectedOrderIds)
                .GroupBy(static item => item.OrderId)
                .ToDictionary(static group => group.Key, static group => group.ToList());

            model.SelectedLaunches = selectedLaunches
                .Select(launch =>
                {
                    historyLookup.TryGetValue(launch.OrderId, out var historyItems);
                    historyItems ??= [];
                    var quantityProduced = historyItems.Sum(static item => item.DeclaredQuantity);

                    var availableMaterialLots = launch.AvailableMaterialLots.ToList();
                    return new Screen4SelectedLaunchViewModel
                    {
                        OrderId = launch.OrderId,
                        LotCode = launch.LotCode,
                        DocumentNumber = launch.DocumentNumber,
                        QuantityToProduce = launch.QuantityToProduce,
                        QuantityProduced = quantityProduced,
                        QuantityDeclared = null,
                        SelectedMaterialLotCode = model.RequiresMaterialLotSelection
                            ? ResolveSelectedOrAutoMaterialLotCode(null, availableMaterialLots)
                            : string.Empty,
                        ArticleCode = launch.ArticleCode,
                        AvailableMaterialLots = availableMaterialLots,
                        MaterialLotValidationMessage = launch.MaterialLotValidationMessage,
                        HasPreviousDeclarations = quantityProduced > 0m && historyItems.Count > 0,
                        PreviousDeclarations = historyItems
                    };
                })
                .ToList();

            model.LineDisplayName = selectedLaunches
                .Select(static launch => launch.LineDescription)
                .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))
                ?? model.LineDisplayName;

            model.ValidationMessage = selectedLaunches
                .Select(static launch => launch.MaterialLotValidationMessage)
                .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

            ApplyLaunchPrefillSelections(model);
            model.TotalDeclared = model.SelectedLaunches.Sum(static item => item.QuantityDeclared ?? 0m);
        }
        catch
        {
            model.ValidationMessage = "Non riesco a rileggere i lotti selezionati.";
        }

        return model;
    }

    private void ApplyProducedQuantities(WorkActionDefinition actionDefinition, string? productionMode, IList<ProductionLaunchItemViewModel> launches)
    {
        if (launches.Count == 0 || string.IsNullOrWhiteSpace(actionDefinition.LineCode))
        {
            return;
        }

        var producedByOrderId = _productionDeclarationPersistenceService
            .GetPreviousDeclarationsByOrderIds(actionDefinition.LineCode!, GetDeclarationPhaseCode(actionDefinition, productionMode), launches.Select(static item => item.OrderId).ToList())
            .GroupBy(static item => item.OrderId)
            .ToDictionary(static group => group.Key, static group => group.Sum(static item => item.DeclaredQuantity));

        foreach (var launch in launches)
        {
            launch.QuantityProduced = producedByOrderId.TryGetValue(launch.OrderId, out var producedQuantity)
                ? producedQuantity
                : 0m;
        }
    }

    private static void ApplyPostedValues(Screen4ViewModel model, Screen4InsertPostModel postModel)
    {
        var declaredQuantities = postModel.GetDeclaredQuantityByOrderId();
        var selectedMaterialLots = postModel.GetSelectedMaterialLotByOrderId();
        foreach (var launch in model.SelectedLaunches)
        {
            if (declaredQuantities.TryGetValue(launch.OrderId, out var declaredQuantity))
            {
                launch.QuantityDeclared = declaredQuantity;
            }

            if (selectedMaterialLots.TryGetValue(launch.OrderId, out var materialLotCode))
            {
                launch.SelectedMaterialLotCode = model.RequiresMaterialLotSelection
                    ? ResolveSelectedOrAutoMaterialLotCode(materialLotCode, launch.AvailableMaterialLots)
                    : string.Empty;
            }
        }

        model.TotalDeclared = model.SelectedLaunches.Sum(static item => item.QuantityDeclared ?? 0m);
        model.GlobalTimingHours = Math.Max(0, postModel.GlobalTimingHours);
        model.GlobalTimingMinutes = Math.Max(0, postModel.GlobalTimingMinutes);
        model.GlobalProblemDescription = postModel.GlobalProblemDescription ?? string.Empty;
        model.GlobalProblemHours = Math.Max(0, postModel.GlobalProblemHours);
        model.GlobalProblemMinutes = Math.Max(0, postModel.GlobalProblemMinutes);
        model.DeclarationDate = postModel.GetDeclarationDateOrDefault(model.DeclarationDate);
        model.ProductionMode = postModel.ProductionMode ?? string.Empty;
        model.ConfirmedOverLimitOrderIds = postModel.ConfirmedOverLimitOrderIds ?? string.Empty;
    }

    private void ApplyLaunchPrefillSelections(Screen4ViewModel model)
    {
        var prefilledSelections = ReadLaunchPrefillSelections();
        if (prefilledSelections.Count == 0 || model.SelectedLaunches.Count == 0)
        {
            return;
        }

        var selectionLookup = prefilledSelections
            .Where(static item => item.OrderId > 0)
            .GroupBy(static item => item.OrderId)
            .ToDictionary(static group => group.Key, static group => group.Last());

        foreach (var launch in model.SelectedLaunches)
        {
            if (!selectionLookup.TryGetValue(launch.OrderId, out var selection))
            {
                continue;
            }

            var declaredQuantity = selection.GetDeclaredQuantity();
            if (declaredQuantity.HasValue && declaredQuantity.Value > 0)
            {
                launch.QuantityDeclared = declaredQuantity.Value;
            }

            launch.SelectedMaterialLotCode = model.RequiresMaterialLotSelection
                ? ResolveSelectedOrAutoMaterialLotCode(selection.SelectedMaterialLotCode, launch.AvailableMaterialLots)
                : string.Empty;
        }
    }

    private List<ProductionLaunchPrefillSelectionItem> ReadLaunchPrefillSelections()
    {
        var rawValue = HttpContext.Session.GetString(LaunchPrefillSelectionsSessionKey);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<ProductionLaunchPrefillSelectionItem>>(rawValue, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private string ReadLaunchPrefillSelectionsJson()
    {
        return SerializeLaunchPrefillSelections(ReadLaunchPrefillSelections());
    }

    private static string SerializeLaunchPrefillSelections(IEnumerable<ProductionLaunchPrefillSelectionItem> items)
    {
        return JsonSerializer.Serialize(items
            .Where(static item => item.OrderId > 0)
            .Select(item => new ProductionLaunchPrefillSelectionItem
            {
                OrderId = item.OrderId,
                QuantityDeclared = item.QuantityDeclared ?? string.Empty,
                SelectedMaterialLotCode = NormalizeMaterialLotCode(item.SelectedMaterialLotCode)
            }), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
    }

    private List<OperatorItemViewModel> LoadSelectedOperatorsFromSession()
    {
        var selectedOperatorIds = HttpContext.Session.GetString(SelectedOperatorIdsSessionKey) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(selectedOperatorIds))
        {
            return [];
        }

        try
        {
            var selectedIds = selectedOperatorIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static value => int.TryParse(value, out var parsedValue) ? parsedValue : (int?)null)
                .Where(static value => value.HasValue)
                .Select(static value => value!.Value)
                .Distinct()
                .OrderBy(static value => value)
                .ToList();

            return _operatorCatalogService.GetByIds(selectedIds).ToList();
        }
        catch
        {
            return [];
        }
    }

    private static string NormalizeMaterialLotCode(string? materialLotCode)
    {
        return (materialLotCode ?? string.Empty).Trim();
    }

    private static string ResolveAvailableMaterialLotCode(string? materialLotCode, IReadOnlyCollection<string>? availableMaterialLots)
    {
        var normalizedValue = NormalizeMaterialLotCode(materialLotCode);
        if (string.IsNullOrWhiteSpace(normalizedValue) || availableMaterialLots is null || availableMaterialLots.Count == 0)
        {
            return string.Empty;
        }

        return availableMaterialLots.FirstOrDefault(value => string.Equals(value, normalizedValue, StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
    }

    private static string ResolveSelectedOrAutoMaterialLotCode(string? materialLotCode, IReadOnlyCollection<string>? availableMaterialLots)
    {
        var resolvedValue = ResolveAvailableMaterialLotCode(materialLotCode, availableMaterialLots);
        if (!string.IsNullOrWhiteSpace(resolvedValue))
        {
            return resolvedValue;
        }

        if (availableMaterialLots is null || availableMaterialLots.Count != 1)
        {
            return string.Empty;
        }

        return NormalizeMaterialLotCode(availableMaterialLots.First());
    }

    private static string? GetBlockingMaterialLotMessage(IEnumerable<ProductionLaunchItemViewModel> launches, IReadOnlyCollection<int> selectedOrderIds)
    {
        return launches
            .Where(item => selectedOrderIds.Contains(item.OrderId))
            .Select(static item => item.MaterialLotValidationMessage)
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
    }

    private static string? GetDeclarationPhaseCode(WorkActionDefinition actionDefinition, string? productionMode)
    {
        if (!IsTrapunteProductionAction(actionDefinition))
        {
            return null;
        }

        return NormalizeProductionMode(productionMode) switch
        {
            ProductionModes.Riempimento => "05",
            ProductionModes.Macchina => "10",
            _ => null
        };
    }

    private ProductionLaunchSelectionViewModel BuildLaunchesViewModel(
        WorkActionDefinition actionDefinition,
        string? productionMode,
        List<OperatorItemViewModel> selectedOperators,
        List<ProductionLaunchItemViewModel> launches,
        IReadOnlyCollection<int> selectedOrderIds,
        bool autoInsertOnBarcodeEnabled,
        string prefilledSelectionsJson,
        string? validationMessage,
        string? successMessage)
    {
        return new ProductionLaunchSelectionViewModel
        {
            ActionId = actionDefinition.Id,
            ActionText = actionDefinition.ActionText,
            AreaTitle = actionDefinition.AreaTitle,
            LineCode = actionDefinition.LineCode!,
            LineDisplayName = launches.Select(static launch => launch.LineDescription).FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value)) ?? actionDefinition.AreaTitle,
            ProductionMode = productionMode ?? string.Empty,
            SelectedOperators = selectedOperators,
            Launches = launches,
            SelectedOrderIds = string.Join(',', selectedOrderIds.OrderBy(static value => value)),
            AutoInsertOnBarcodeEnabled = autoInsertOnBarcodeEnabled,
            RequiresMaterialLotSelection = RequiresMaterialLotSelection(actionDefinition, productionMode),
            AvailableMaterialLots = [],
            PrefilledSelectionsJson = prefilledSelectionsJson,
            ValidationMessage = validationMessage,
            SuccessMessage = successMessage
        };
    }

    private List<int> ReadSelectedLaunchOrderIds()
    {
        var rawValue = HttpContext.Session.GetString(SelectedLaunchOrderIdsSessionKey);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return [];
        }

        return rawValue
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(static value => int.TryParse(value, out var parsedValue) ? parsedValue : (int?)null)
            .Where(static value => value.HasValue)
            .Select(static value => value!.Value)
            .Distinct()
            .OrderBy(static value => value)
            .ToList();
    }

    private string? ResolveAndPersistProductionMode(WorkActionDefinition actionDefinition, string? rawProductionMode)
    {
        if (!IsTrapunteProductionAction(actionDefinition))
        {
            HttpContext.Session.Remove(ProductionModeSessionKey);
            return null;
        }

        var normalizedProductionMode = NormalizeProductionMode(rawProductionMode);
        if (string.IsNullOrWhiteSpace(normalizedProductionMode))
        {
            normalizedProductionMode = GetSelectedProductionMode();
        }

        if (string.IsNullOrWhiteSpace(normalizedProductionMode))
        {
            HttpContext.Session.Remove(ProductionModeSessionKey);
            return null;
        }

        HttpContext.Session.SetString(ProductionModeSessionKey, normalizedProductionMode);
        return normalizedProductionMode;
    }

    private string? GetSelectedProductionMode()
    {
        return NormalizeProductionMode(HttpContext.Session.GetString(ProductionModeSessionKey));
    }

    private bool IsDateEditAuthorized(IEnumerable<OperatorItemViewModel> selectedOperators)
    {
        return string.Equals(HttpContext.Session.GetString(DateEditAuthorizedSessionKey), "1", StringComparison.Ordinal)
            && _declarationDateAuthorizationService.CanAnySelectedOperatorEditDate(selectedOperators.Select(static item => item.Id));
    }

    private void ClearCurrentFlow()
    {
        HttpContext.Session.Remove(SelectedOperatorIdsSessionKey);
        HttpContext.Session.Remove(SelectedActionIdSessionKey);
        HttpContext.Session.Remove(SelectedLaunchOrderIdsSessionKey);
        HttpContext.Session.Remove(ProductionModeSessionKey);
        HttpContext.Session.Remove(AutoFillMaxFromBarcodeSessionKey);
        HttpContext.Session.Remove(AutoInsertOnBarcodeSessionKey);
        HttpContext.Session.Remove(LaunchPrefillSelectionsSessionKey);
        HttpContext.Session.Remove(DateEditAuthorizedSessionKey);
    }

    private static bool IsTrapunteProductionAction(WorkActionDefinition actionDefinition)
    {
        return string.Equals(actionDefinition.Id, "trapunte-dichiarazione-produzione", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequiresMaterialLotSelection(WorkActionDefinition actionDefinition, string? productionMode)
    {
        if (actionDefinition.FlowType != WorkFlowType.ProductionLaunches)
        {
            return false;
        }

        return !(IsTrapunteProductionAction(actionDefinition)
            && string.Equals(NormalizeProductionMode(productionMode), ProductionModes.Macchina, StringComparison.Ordinal));
    }

    private static bool ShouldResolveMaterialLots(WorkActionDefinition actionDefinition)
    {
        return actionDefinition.FlowType == WorkFlowType.ProductionLaunches;
    }

    private static string? NormalizeProductionMode(string? productionMode)
    {
        var normalizedValue = (productionMode ?? string.Empty).Trim().ToLowerInvariant();
        return normalizedValue switch
        {
            ProductionModes.Riempimento => ProductionModes.Riempimento,
            ProductionModes.Macchina => ProductionModes.Macchina,
            _ => null
        };
    }

    private static bool TryGetWorkAction(string actionId, out WorkActionDefinition definition)
    {
        return WorkActionMap.TryGetValue(actionId, out definition!);
    }

    private sealed record WorkActionDefinition(
        string Id,
        string AreaTitle,
        string ActionText,
        string FlowType,
        string? LineCode);

    private static class WorkFlowType
    {
        public const string ProductionLaunches = "production-launches";
        public const string Screen4Direct = "screen4-direct";
        public const string Pending = "pending";
    }

    private static class ProductionModes
    {
        public const string Riempimento = "riempimento";
        public const string Macchina = "macchina";
    }
}
