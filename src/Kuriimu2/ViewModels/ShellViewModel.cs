﻿using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using Kontract.Interfaces;
using Kontract.Interfaces.Common;
using Kontract.Interfaces.Font;
using Kontract.Interfaces.Image;
using Kontract.Interfaces.Text;
using Kore;
using Kore.SamplePlugins;
using Kore.Utilities;
using Kuriimu2.Dialogs.ViewModels;
using Kuriimu2.Interfaces;
using Microsoft.Win32;

namespace Kuriimu2.ViewModels
{
    public sealed class ShellViewModel : Conductor<IScreen>.Collection.OneActive
    {
        #region Private

        private IWindowManager _wm = new WindowManager();
        private List<IScreen> _windows = new List<IScreen>();
        private Kore.KoreManager _kore;

        #endregion

        public ShellViewModel()
        {
            DisplayName = "Kuriimu2";
            _kore = new Kore.KoreManager();

            // Load passed-in file
            if (AppBootstrapper.Args.Length > 0 && File.Exists(AppBootstrapper.Args[0]))
                LoadFile(AppBootstrapper.Args[0]);
        }

        public async void OpenButton()
        {
            var ofd = new OpenFileDialog { Filter = _kore.FileFilters, Multiselect = true };
            if (ofd.ShowDialog() != true) return;

            foreach (var file in ofd.FileNames)
                await LoadFile(file);
        }

        public async void OpenTypeButton()
        {
            var pe = new OpenTypeViewModel(_kore)
            {
                Title = $"Open File by Type",
                Message = ""
            };
            _windows.Add(pe);

            if (_wm.ShowDialog(pe) == true)
            {
                await LoadFile(pe.SelectedFilePath, pe.SelectedFormatType);
            }
        }

        public async void FileDrop(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null) return;

            foreach (var file in files)
                await LoadFile(file);
        }

        public bool SaveButtonsEnabled => (ActiveItem as IFileEditor)?.KoreFile.Adapter is ISaveFiles;

        public void SaveButton()
        {
            SaveFile();
        }

        public void SaveAsButton()
        {
            var filter = "Any File (*.*)|*.*";

            if (ActiveItem is IFileEditor editor)
            {
                filter = editor.KoreFile.Filter;

                var sfd = new SaveFileDialog { FileName = editor.KoreFile.FileInfo.Name, Filter = filter };
                if (sfd.ShowDialog() != true) return;

                SaveFile(sfd.FileName);
            }
            else
            {

            }
        }

        public void DebugButton()
        {
            //_kore.Debug();
        }

        // Toolbars
        public Visibility TextEditorToolsVisible => ActiveItem is TextEditor2ViewModel ? Visibility.Visible : Visibility.Hidden;

        public void TextEditorExportFile()
        {
            // TODO: Introduce a way to select from a list of ITextAdapter + ICreateFiles to allow for format conversion
            var editor = (IFileEditor)ActiveItem;
            if (!(editor.KoreFile.Adapter is ITextAdapter adapter)) return;

            var sfd = new SaveFileDialog
            {
                FileName = editor.KoreFile.FileInfo.Name + Kore.Utilities.Common.GetAdapterExtension<KupAdapter>(),
                Filter = Kore.Utilities.Common.GetAdapterFilter<KupAdapter>()
            };
            if (sfd.ShowDialog() != true) return;

            Kore.Utilities.Text.ExportKup(adapter, sfd.FileName);
        }

        public void TextEditorImportFile()
        {
            var editor = (IFileEditor)ActiveItem;
            if (!(editor.KoreFile.Adapter is ITextAdapter adapter)) return;

            var ofd = new OpenFileDialog { Filter = _kore.FileFiltersByType<ITextAdapter>("All Supported Text Files") };
            if (ofd.ShowDialog() != true) return;

            editor.KoreFile.HasChanges = _kore.ImportFile(adapter, ofd.FileName);
        }

        // Tabs
        public void TabChanged(SelectionChangedEventArgs args)
        {
            NotifyOfPropertyChange(() => TextEditorToolsVisible);
            NotifyOfPropertyChange(() => SaveButtonsEnabled);
        }

        public void CloseTab(IScreen tab)
        {
            tab.TryClose();
            switch (tab)
            {
                case IFileEditor editor:
                    _kore.CloseFile(editor.KoreFile);
                    break;
            }
        }

        public void CloseAllTabs()
        {
            for (var i = Items.Count - 1; i >= 0; i--)
                CloseTab(Items[i]);
        }

        #region Private Methods

        private async Task LoadFile(string filename)
        {
            KoreFileInfo kfi = null;

            try
            {
                await Task.Run(() => { kfi = _kore.LoadFile(filename, true); });
            }
            catch (LoadFileException ex)
            {
                MessageBox.Show(ex.ToString(), "Open File", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ActivateTab(kfi);
        }

        private async Task LoadFile(string filename, ILoadFiles adapter)
        {
            KoreFileInfo kfi = null;

            try
            {
                await Task.Run(() => { kfi = _kore.LoadFile(filename, adapter); });
            }
            catch (LoadFileException ex)
            {
                MessageBox.Show(ex.ToString(), "Open File", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            ActivateTab(kfi);
        }

        private void ActivateTab(KoreFileInfo kfi)
        {
            if (kfi == null) return;

            switch (kfi.Adapter)
            {
                case ITextAdapter txt2:
                    ActivateItem(new TextEditor2ViewModel(_kore, kfi));
                    break;
                case IImageAdapter img:
                    ActivateItem(new ImageEditorViewModel(_kore, kfi));
                    break;
                case IFontAdapter fnt:
                    ActivateItem(new FontEditorViewModel(kfi));
                    break;
            }
        }

        private void SaveFile(string filename = "")
        {
            (ActiveItem as IFileEditor)?.Save(filename);
        }

        #endregion

        public override void TryClose(bool? dialogResult = null)
        {
            for (var i = _windows.Count - 1; i >= 0; i--)
            {
                var scr = _windows[i];
                scr.TryClose(dialogResult);
                _windows.Remove(scr);
            }
            base.TryClose(dialogResult);
        }
    }
}
