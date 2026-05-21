using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public interface IDeclarationNoteTypeCatalogService
{
    IReadOnlyList<DeclarationNoteTypeViewModel> GetForGenericDeclarations();
    IReadOnlyList<DeclarationNoteTypeViewModel> GetForProductionDeclarations();
}
