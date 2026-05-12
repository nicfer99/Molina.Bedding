using Molina.Bedding.Mvc.Models;

namespace Molina.Bedding.Mvc.Services;

public class StaticWorkMenuService : IWorkMenuService
{
    public IReadOnlyList<WorkAreaViewModel> GetInitialAreas()
    {
        return
        [
            new WorkAreaViewModel
            {
                Id = "linea-cassette",
                Title = "Linea Kassetten",
                Actions =
                [
                    new WorkAreaActionViewModel
                    {
                        Id = "cassette-dichiarazione-produzione",
                        Text = "Dichiarazione produzione",
                        Icon = "check",
                        Tone = "success",
                        Row = 1
                    },
                    new WorkAreaActionViewModel
                    {
                        Id = "cassette-pulizia",
                        Text = "Pulizia",
                        Icon = "preferences",
                        Tone = "normal",
                        Row = 3
                    }
                ]
            },
            new WorkAreaViewModel
            {
                Id = "linea-trapunte",
                Title = "Linea Trapunte",
                Actions =
                [
                    new WorkAreaActionViewModel
                    {
                        Id = "trapunte-dichiarazione-produzione",
                        Text = "Dichiarazione produzione",
                        Icon = "check",
                        Tone = "success",
                        Row = 1
                    },
                    new WorkAreaActionViewModel
                    {
                        Id = "trapunte-setup-2f",
                        Text = "Setup 2F",
                        Icon = "preferences",
                        Tone = "default",
                        Row = 2
                    }
                ]
            },
            new WorkAreaViewModel
            {
                Id = "linea-guanciali",
                Title = "Linea Guanciali",
                Actions =
                [
                    new WorkAreaActionViewModel
                    {
                        Id = "guanciali-dichiarazione-produzione",
                        Text = "Dichiarazione produzione",
                        Icon = "check",
                        Tone = "success",
                        Row = 1
                    },
                    new WorkAreaActionViewModel
                    {
                        Id = "guanciali-pulizia",
                        Text = "Pulizia",
                        Icon = "preferences",
                        Tone = "normal",
                        Row = 3
                    }
                ]
            },
            new WorkAreaViewModel
            {
                Id = "pavimento",
                Title = "Pavimento",
                Actions =
                [
                    new WorkAreaActionViewModel
                    {
                        Id = "pavimento-pulizia",
                        Text = "Pulizia",
                        Icon = "preferences",
                        Tone = "normal",
                        Row = 3
                    }
                ]
            }
        ];
    }
}
