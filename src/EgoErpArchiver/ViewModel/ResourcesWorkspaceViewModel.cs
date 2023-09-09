using EgoEngineLibrary.Archive.Erp;
using EgoEngineLibrary.Formats.Erp;
using Ookii.Dialogs.Wpf;
using Microsoft.Toolkit.HighPerformance.Buffers;
using Microsoft.VisualBasic;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml.XPath;
using System.Linq;

namespace EgoErpArchiver.ViewModel
{
    public class ResourcesWorkspaceViewModel : WorkspaceViewModel
    {
        private readonly ErpResourceExporter resourceExporter;

        private readonly ObservableCollection<ErpResourceViewModel> resources;
        public ObservableCollection<ErpResourceViewModel> Resources
        {
            get { return resources; }
        }

        private string _displayName;
        public override string DisplayName
        {
            get { return _displayName; }
            protected set
            {
                _displayName = value;
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        private ErpResourceViewModel selectedItem;
        public ErpResourceViewModel SelectedItem
        {
            get { return selectedItem; }
            set
            {
                if (!object.ReferenceEquals(value, selectedItem))
                {
                    selectedItem = value;
                    OnPropertyChanged(nameof(SelectedItem));
                }
            }
        }

        public RelayCommand RenameAll { get; }
        public RelayCommand Rename { get; }
        public RelayCommand Repath { get; }
        public RelayCommand Export { get; }
        public RelayCommand Import { get; }
        public RelayCommand ExportAll { get; }
        public RelayCommand ExportAllFilter { get; }
        public RelayCommand ImportAll { get; }
        public RelayCommand ChangeType { get; }

        public RelayCommand DuplicateResource { get; }
        public RelayCommand RemoveResource { get; }
        public RelayCommand AddFragment { get; }
        public RelayCommand RemoveFragment { get; }

        public ResourcesWorkspaceViewModel(MainViewModel mainView)
            : base(mainView)
        {
            resourceExporter = new ErpResourceExporter();
            resources = new ObservableCollection<ErpResourceViewModel>();
            _displayName = "All Resources";

            RenameAll = new RelayCommand(RenameAll_Execute, ExportAll_CanExecute);
            Rename = new RelayCommand(Rename_Execute, Rename_CanExecute);
            Repath = new RelayCommand(Repath_Execute, Rename_CanExecute);
            Export = new RelayCommand(Export_Execute, Export_CanExecute);
            Import = new RelayCommand(Import_Execute, Import_CanExecute);
            ExportAll = new RelayCommand(ExportAll_Execute, ExportAll_CanExecute);
            ExportAllFilter = new RelayCommand(ExportAllFilter_Execute, ExportAll_CanExecute);
            ImportAll = new RelayCommand(ImportAll_Execute, ImportAll_CanExecute);
            ChangeType = new RelayCommand(ChangeType_Execute, Rename_CanExecute);

            DuplicateResource = new RelayCommand(DuplicateResource_Execute, Rename_CanExecute);
            RemoveResource = new RelayCommand(RemoveResource_Execute, Rename_CanExecute);
            AddFragment = new RelayCommand(AddFragment_Execute, Rename_CanExecute);
            RemoveFragment = new RelayCommand(RemoveFragment_Execute, Rename_CanExecute);
        }

        public override void LoadData(object data)
        {
            foreach (var resource in ((ErpFile)data).Resources)
            {
                resources.Add(new ErpResourceViewModel(resource, this));
            }
            DisplayName = "All Resources " + resources.Count;
        }

        public override void ClearData()
        {
            resources.Clear();
        }

        private void RenameAll_Execute(object parameter)
        {
            string src = Interaction.InputBox("Enter string to find:");
            if (string.IsNullOrWhiteSpace(src))
                return;

            string dest = Interaction.InputBox("Enter string to replace:");
            if (string.IsNullOrWhiteSpace(dest))
                return;

            foreach (ErpResource res in mainView.ErpFile.Resources)
                res.Identifier = res.Identifier.Replace(src, dest);

            mainView.ErpFile.UpdateOffsets();
            mainView.UpdateWorkspace();

            MessageBox.Show("Names updated!");
        }

        private bool Rename_CanExecute(object parameter)
        {
            return parameter != null;
        }

        private void Rename_Execute(object parameter)
        {
            var resView = (ErpResourceViewModel)parameter;
            var res = resView.Resource;

            string result = Interaction.InputBox(
                Prompt: "Enter a new name for this resource:",
                Title: "Rename resource",
                DefaultResponse: res.FileName);

            if (string.IsNullOrWhiteSpace(result))
                return;

            res.FileName = result;
            mainView.ErpFile.UpdateOffsets();
            mainView.UpdateWorkspace();
        }

        private void DuplicateResource_Execute(object parameter)
        {
            ErpResourceViewModel resView = (ErpResourceViewModel)parameter;
            ErpResource old = resView.Resource;
            ErpResource res = new(old.ParentFile);

            foreach (ErpFragment oldFrag in old.Fragments)
            {
                ErpFragment frag = new(res.ParentFile);
                frag.SetData(oldFrag.GetDataArray(true), compress: true);
                res.Fragments.Add(frag);
            }

            res.Identifier = old.Identifier;
            res.FileName = old.FileName;
            res.ResourceType = old.ResourceType;

            mainView.ErpFile.Resources.Add(res);
            mainView.ErpFile.UpdateOffsets();
            mainView.UpdateWorkspace();
        }

        private void RemoveResource_Execute(object parameter)
        {
            ErpResourceViewModel resView = (ErpResourceViewModel)parameter;
            ErpResource res = resView.Resource;

            mainView.ErpFile.Resources.Remove(res);
            mainView.ErpFile.UpdateOffsets();
            mainView.UpdateWorkspace();
        }

        private void AddFragment_Execute(object parameter)
        {
            ErpResourceViewModel resView = (ErpResourceViewModel)parameter;
            ErpResource res = resView.Resource;

            ErpFragment data = new ErpFragment(res.ParentFile);
            data.SetData( res.Fragments.Last().GetDataArray(true), compress: true );

            res.Fragments.Add(data);
            mainView.ErpFile.UpdateOffsets();
            mainView.UpdateWorkspace();
        }

        private void RemoveFragment_Execute(object parameter)
        {
            ErpResourceViewModel resView = (ErpResourceViewModel)parameter;
            ErpResource res = resView.Resource;

            if (res.Fragments.Count > 1)
            {
                res.Fragments.RemoveAt(res.Fragments.Count-1);
                mainView.ErpFile.UpdateOffsets();
                mainView.UpdateWorkspace();
            }
        }

        private void Repath_Execute(object parameter)
        {
            var resView = (ErpResourceViewModel)parameter;
            var res = resView.Resource;

            string result = Interaction.InputBox(
                Prompt: "Enter a new URI for this resource:",
                Title: "Change resource URI",
                DefaultResponse: res.Identifier);

            if (string.IsNullOrWhiteSpace(result))
                return;

            res.Identifier = result;
            mainView.ErpFile.UpdateOffsets();
            mainView.UpdateWorkspace();
        }

        private bool Export_CanExecute(object parameter)
        {
            return parameter != null;
        }

        private void Export_Execute(object parameter)
        {
            var resView = (ErpResourceViewModel)parameter;
            var dlg = new VistaFolderBrowserDialog
            {
                Description = "Select a folder to export the resource:",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    resourceExporter.ExportResource(resView.Resource, dlg.SelectedPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed Exporting!" + Environment.NewLine + Environment.NewLine +
                        ex.Message, Properties.Resources.AppTitleLong, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool Import_CanExecute(object parameter)
        {
            return parameter != null;
        }

        private void Import_Execute(object parameter)
        {
            var resView = (ErpResourceViewModel)parameter;
            var dlg = new VistaFolderBrowserDialog
            {
                Description = "Select a folder to import the resource from:",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var files = Directory.GetFiles(dlg.SelectedPath, "*", SearchOption.AllDirectories);
                    resourceExporter.ImportResource(resView.Resource, files);
                    resView.UpdateSize();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed Importing!" + Environment.NewLine + Environment.NewLine +
                        ex.Message, Properties.Resources.AppTitleLong, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Prompt user to enter a file filter.
        /// </summary>
        /// <param name="promptFilter"></param>
        private void ExportAllFunc(bool promptFilter)
        {
            string filter = "";

            if (promptFilter)
            {
                filter = Interaction.InputBox(
                    Prompt: "Enter a suffix filter, e.g. '.material' for all files\n" +
                    "with the .material extension",
                    Title: "Filter");
                if (string.IsNullOrWhiteSpace(filter))
                    return;
            }
            var dlg = new VistaFolderBrowserDialog
            {
                Description = "Select a folder to export the resources:",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var progDialogVM = new ProgressDialogViewModel()
                    {
                        PercentageMax = mainView.ErpFile.Resources.Count
                    };
                    var progDialog = new View.ProgressDialog
                    {
                        DataContext = progDialogVM
                    };

                    var task = Task.Run(() => resourceExporter.Export(mainView.ErpFile, dlg.SelectedPath,
                        progDialogVM.ProgressStatus, progDialogVM.ProgressPercentage,
                        filter: filter));

                    progDialog.ShowDialog();
                    task.Wait();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed Exporting!" + Environment.NewLine + Environment.NewLine +
                        ex.Message, Properties.Resources.AppTitleLong, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool ExportAll_CanExecute(object parameter)
        {
            return resources.Count > 0;
        }

        private void ExportAllFilter_Execute(object parameter)
        {
            ExportAllFunc(promptFilter: true);
        }

        private void ExportAll_Execute(object parameter)
        {
            ExportAllFunc(promptFilter: false);
        }

        private bool ImportAll_CanExecute(object parameter)
        {
            return resources.Count > 0;
        }

        private void ImportAll_Execute(object parameter)
        {
            var dlg = new VistaFolderBrowserDialog
            {
                Description = "Select a folder to import the resources from:",
                Multiselect = false
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    var progDialogVM = new ProgressDialogViewModel()
                    {
                        PercentageMax = mainView.ErpFile.Resources.Count
                    };
                    var progDialog = new View.ProgressDialog
                    {
                        DataContext = progDialogVM
                    };

                    var files = Directory.GetFiles(dlg.SelectedPath, "*", SearchOption.AllDirectories);
                    var task = Task.Run(() => resourceExporter.Import(mainView.ErpFile, files, progDialogVM.ProgressStatus, progDialogVM.ProgressPercentage));
                    progDialog.ShowDialog();
                    task.Wait();

                    foreach (var child in resources)
                    {
                        child.UpdateSize();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed Importing!" + Environment.NewLine + Environment.NewLine +
                        ex.Message, Properties.Resources.AppTitleLong, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ChangeType_Execute(object parameter)
        {
            string choice = Interaction.InputBox("Enter new type");
            if (string.IsNullOrWhiteSpace(choice))
                return;
            var resView = (ErpResourceViewModel)parameter;
            resView.Resource.ResourceType = choice;
            mainView.ErpFile.UpdateOffsets();
            mainView.UpdateWorkspace();
        }
    }
}
