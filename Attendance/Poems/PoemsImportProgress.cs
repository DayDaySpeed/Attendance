using Attendance.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Attendance.Poems
{
    public class PoemsImportProgress : ObservableObject
    {
        private int _total;
        private int _current;
        private string _status;
        private Visibility _visibility = Visibility.Visible;

        public int Total
        {
            get => _total;
            set => SetProperty(ref _total, value);
        }

        public int Current
        {
            get => _current;
            set => SetProperty(ref _current, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public Visibility Visibility
        {
            get => _visibility;
            set => SetProperty(ref _visibility, value);
        }
    }
}
