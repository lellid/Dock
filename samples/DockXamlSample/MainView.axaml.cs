using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Dock.Avalonia.Controls;
using Dock.Model;
using Dock.Model.Avalonia.Controls;
using Dock.Model.Core;
using Dock.Serializer;

namespace DockXamlSample;

public partial class MainView : UserControl
{
    private readonly IDockSerializer _serializer;
    private readonly IDockState _dockState;

    public MainView()
    {
        InitializeComponent();

        _serializer = new DockSerializer(typeof(AvaloniaList<>));
        // _serializer = new AvaloniaDockSerializer();

        _dockState = new DockState();

        if (Dock is { })
        {
            var layout = Dock.Layout;
            if (layout is { })
            {
                _dockState.Save(layout);
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private List<FilePickerFileType> GetOpenOpenLayoutFileTypes()
    {
        return new List<FilePickerFileType>
        {
            StorageService.Json,
            StorageService.All
        };
    }

    private List<FilePickerFileType> GetSaveOpenLayoutFileTypes()
    {
        return new List<FilePickerFileType>
        {
            StorageService.Json,
            StorageService.All
        };
    }

    private async Task OpenLayout()
    {
        var storageProvider = StorageService.GetStorageProvider();
        if (storageProvider is null)
        {
            return;
        }

        var result = await storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open layout",
            FileTypeFilter = GetOpenOpenLayoutFileTypes(),
            AllowMultiple = false
        });

        var file = result.FirstOrDefault();

        if (file is not null)
        {
            try
            {
                await using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                var dock = this.FindControl<DockControl>("Dock");
                if (dock is { })
                {
                    var layout = _serializer.Load<IDock?>(stream);
                    // TODO:
                    // var layout = await JsonSerializer.DeserializeAsync(
                    //     stream, 
                    //     AvaloniaDockSerializer.s_serializerContext.RootDock);
                    if (layout is { })
                    {
                        dock.Layout = layout;
                        _dockState.Restore(layout);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private async Task SaveLayout()
    {
        var storageProvider = StorageService.GetStorageProvider();
        if (storageProvider is null)
        {
            return;
        }

        var file = await storageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save layout",
            FileTypeChoices = GetSaveOpenLayoutFileTypes(),
            SuggestedFileName = "layout",
            DefaultExtension = "json",
            ShowOverwritePrompt = true
        });

        if (file is not null)
        {
            try
            {
                await using var stream = await file.OpenWriteAsync();
                var dock = this.FindControl<DockControl>("Dock");
                if (dock?.Layout is { })
                {
                    _serializer.Save(stream, dock.Layout);
                    // TODO:
                    // await JsonSerializer.SerializeAsync(
                    //     stream, 
                    //     (RootDock)dock.Layout, AvaloniaDockSerializer.s_serializerContext.RootDock);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    private void CloseLayout()
    {
        var dock = this.FindControl<DockControl>("Dock");
        if (dock is { })
        {
            dock.Layout = null;
        }
    }

    private async void FileOpenLayout_OnClick(object? sender, RoutedEventArgs e)
    {
        await OpenLayout();
    }

    private async void FileSaveLayout_OnClick(object? sender, RoutedEventArgs e)
    {
        await SaveLayout();
    }

    private void FileCloseLayout_OnClick(object? sender, RoutedEventArgs e)
    {
        CloseLayout();
    }

    /// <summary>
    /// Called if the user chooses File->New tool from the main menu.
    /// A new tool is added to the right side (but only if it exists).
    /// </summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
    private void FileNewTool_OnClick(object? sender, RoutedEventArgs e)
    {
        if (this.FindControl<DockControl>("Dock") is { } dock &&
            dock.Factory is { } factory)
        {
            foreach (var dockable in factory.VisibleDockableControls.ToArray())
            {
                if (dockable.Key is ToolDock tooldock && tooldock.Alignment == Alignment.Right)
                {
                    // create a new ViewModel
                    var newToolModel = new Tool();
                    newToolModel.Id = "ID of MenuTool";
                    newToolModel.Title = "MenuTool";

                    // provide the ViewModel with a method to create its view
                    newToolModel.Content = new Func<object, TemplateResult<Control>>((data) => GetViewForViewModel(newToolModel));

                    // add the view model
                    factory.AddDockable(tooldock, newToolModel);
                    factory.SetActiveDockable(newToolModel);
                    break;
                }

            }
        }
    }

    /// <summary>
    /// Gets the view for the given view model.
    /// </summary>
    /// <param name="viewModel">The view model.</param>
    /// <returns></returns>
    private TemplateResult<Control> GetViewForViewModel(object viewModel)
    {
        var view = new MenuToolView() { DataContext = viewModel };
        var result = new TemplateResult<Control>((Control)view, null);
        return result;
    }
}

