using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Altus.Core.Presentation.ViewModels;
using Altus.Core.Presentation.WPF.Runtime;

namespace Altus.Core.Presentation.WPF.Converters
{
    public class ViewStyleConverter : IMultiValueConverter
    {
        public virtual ViewStyleSelector CreateStyleSelector(View view)
        {
            return new ViewStyleSelector(view);
        }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(values[0] is DependencyObject && values[1] is View)) return Binding.DoNothing;
            ViewStyleSelector selector = CreateStyleSelector((View)values[1]);
            return selector.SelectStyle((View)values[1], (DependencyObject)values[0]); throw new NotImplementedException();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
