using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KasPriceChart
{
    internal class CustomControls
    {
        public class NoWheelTrackBar : TrackBar
        {
            protected override void OnMouseWheel(MouseEventArgs e)
            { // Do nothing to ignore mouse wheel input
            }
        }
    }
}
