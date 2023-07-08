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
        public RelayCommand AddFragment { get; }

        public RelayCommand QuickF12021_DriverClothes { get; }
        public RelayCommand QuickF12021_ApplyHeadSwap { get; }
        public RelayCommand QuickF12021_RevertHeadSwap { get; }

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
            AddFragment = new RelayCommand(AddFragment_Execute, Rename_CanExecute);

            QuickF12021_DriverClothes = new RelayCommand(QuickF12021_DriverClothes_Execute, ExportAll_CanExecute);
            QuickF12021_ApplyHeadSwap = new RelayCommand(QuickHeadSwapApply_Execute, ExportAll_CanExecute);
            QuickF12021_RevertHeadSwap = new RelayCommand(QuickHeadSwapRevert_Execute, ExportAll_CanExecute);
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

        private void AddFragment_Execute(object parameter)
        {
            ErpResourceViewModel resView = (ErpResourceViewModel)parameter;
            ErpResource res = resView.Resource;

            res.Fragments.Add( res.Fragments.Last() );
            mainView.ErpFile.UpdateOffsets();
            mainView.UpdateWorkspace();
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

        private void QuickHeadSwapApply_Execute(object parameter)
        {
            GenderHeadSwap(forward: true);
        }

        private void QuickHeadSwapRevert_Execute(object parameter)
        {
            GenderHeadSwap(forward: false);
        }

        private void GenderHeadSwap(bool forward=true)
        {
            string maleDriver = Interaction.InputBox("Enter name of male driver\n(e.g. carlos_sainz-jr)");
            if (string.IsNullOrWhiteSpace(maleDriver))
                return;
            string femaleDriver = Interaction.InputBox("Enter name of female driver\n(e.g. ginny_gladwell)");
            if (string.IsNullOrWhiteSpace(femaleDriver))
                return;

            var resourceIDs = new (string male, string female)[]
            {
                (
                    $"eaid://character_package/heads/male_driver/{maleDriver}/idf/{maleDriver}_rig_head_morph_v1.emb?context=default",
                    $"eaid://character_package/heads/female_generic/{femaleDriver}/idf/{femaleDriver}_rig_head_morph_v2.emb?context=default"
                ),

                (
                    $"eaid://character_package/heads/male_driver/{maleDriver}/idf/{maleDriver}_rig_head_morph_v1.idf?context=animset",
                    $"eaid://character_package/heads/female_generic/{femaleDriver}/idf/{femaleDriver}_rig_head_morph_v2.idf?context=animset"
                ),

                (
                    $"eaid://character_package/heads/male_driver/{maleDriver}/idf/{maleDriver}_rig_head_morph_v1.idf?model",
                    $"eaid://character_package/heads/female_generic/{femaleDriver}/idf/{femaleDriver}_rig_head_morph_v2.idf?model"
                ),

                (
                    $"eaid://character_package/heads/male_driver/{maleDriver}/idf/{maleDriver}_rig_head_morph_v1.idf?render",
                    $"eaid://character_package/heads/female_generic/{femaleDriver}/idf/{femaleDriver}_rig_head_morph_v2.idf?render"
                )
            };

            // First, validate that source resources exist
            foreach (var pair in resourceIDs)
            {
                string src = forward ? pair.female : pair.male;
                string dest = forward ? pair.male : pair.female;

                ErpResource result = mainView.ErpFile.Resources.Find(x => x.Identifier == src);
                if (result == null)
                {
                    MessageBox.Show($"Could not find resource:\n{src}");
                    return;
                }

                result = mainView.ErpFile.Resources.Find(x => x.Identifier == dest);
                if (result != null)
                {
                    MessageBox.Show($"Destination file already exists:\n{dest}");
                    return;
                }
            }

            foreach (var pair in resourceIDs)
            {
                string src = forward ? pair.female : pair.male;
                string dest = forward ? pair.male : pair.female;
                ErpResource resource = mainView.ErpFile.FindResource(src);
                resource.Identifier = dest;
            }

            mainView.ErpFile.UpdateOffsets();
            mainView.UpdateWorkspace();

            MessageBox.Show("URIs updated!");
        }

        private void QuickF12021_DriverClothes_Execute(object parameter)
        {
            string destDriver = Interaction.InputBox("Enter name of new driver\n(e.g. carlos_sainz-jr)");
            if (string.IsNullOrWhiteSpace(destDriver))
                return;
            string destTeam = Interaction.InputBox("Enter year-team of new driver\n(e.g. 2020-haas)");
            if (string.IsNullOrWhiteSpace(destTeam))
                return;
            string srcDriver = Interaction.InputBox("Enter driver being replaced\n(e.g. carlos_sainz-jr)");
            if (string.IsNullOrWhiteSpace(srcDriver))
                return;
            string srcTeam = Interaction.InputBox("Enter year-team of driver being replaced\n(e.g. 2021-haas)");
            if (string.IsNullOrWhiteSpace(srcTeam))
                return;

            string srcFull = srcTeam + '_' + srcDriver;
            string destFull = destTeam + '_' + destDriver;

            string[] resourceIDs = new string[]
            {
                $"eaid://character_package/condition_scene/idf/driver_body_v2_male.emb?context={destFull}",
                $"eaid://character_package/condition_scene/idf/driver_gloves.emb?context={destFull}",

                $"eaid://character_package/drivers/male/{destFull}/idf/{destDriver}_body_logos.emb?context=default",
                $"eaid://character_package/drivers/male/{destFull}/idf/{destDriver}_body_logos.idf?model",
                $"eaid://character_package/drivers/male/{destFull}/idf/{destDriver}_body_logos.idf?render",

                $"eaid://character_package/drivers/male/{destFull}/idf/{destDriver}_glove_logos.emb?context=default",
                $"eaid://character_package/drivers/male/{destFull}/idf/{destDriver}_glove_logos.idf?model",
                $"eaid://character_package/drivers/male/{destFull}/idf/{destDriver}_glove_logos.idf?render"
            };

            // First, validate that all these resources exists
            foreach (string resID in resourceIDs)
            {
                ErpResource result = mainView.ErpFile.Resources.Find(x => x.Identifier == resID);
                if (result == null)
                {
                    MessageBox.Show($"Could not find resource:\n{resID}");
                    return;
                }
            }

            foreach (string resID in resourceIDs)
            {
                /// How to handle resource not found?
                ErpResource resource = mainView.ErpFile.FindResource(resID);
                string id = resource.Identifier;
                id = id.Replace(destFull, srcFull);
                id = id.Replace(destDriver, srcDriver);
                resource.Identifier = id;
            }

            mainView.ErpFile.UpdateOffsets();
            mainView.UpdateWorkspace();

            MessageBox.Show("URIs updated!");
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
