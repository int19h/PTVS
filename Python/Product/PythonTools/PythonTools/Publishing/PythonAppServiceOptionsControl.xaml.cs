using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Microsoft.PythonTools.Publishing {
    /// <summary>
    /// Interaction logic for PythonAppServiceOptionsControl.xaml
    /// </summary>
    public partial class PythonAppServiceOptionsControl : UserControl, INotifyPropertyChanged {
        public PythonAppServiceOptionsControl() {
            DataContext = this;
            InitializeComponent();
        }

        private bool _createNewService;

        public bool CreateNewService {
            get => _createNewService;
            set {
                if (_createNewService != value) {
                    _createNewService = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CreateNewService)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UseExistingService)));
                }
            }
        }

        public bool UseExistingService {
            get => !CreateNewService;
            set => CreateNewService = !value;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
