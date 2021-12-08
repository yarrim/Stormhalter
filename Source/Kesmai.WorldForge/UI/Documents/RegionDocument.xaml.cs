using System.Windows.Controls;
using Microsoft.Toolkit.Mvvm.Messaging;
using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using Kesmai.WorldForge.Editor;

namespace Kesmai.WorldForge.UI.Documents
{
	public partial class RegionDocument : UserControl
	{
		public class ExportRegionRequest {
			public string FileName;
			public SegmentRegion Region;
			public int Width, Height, Top, Left;
			public ExportRegionRequest (string filename, SegmentRegion region, int width, int height, int top, int left)
            {
				FileName = filename;
				Region = region;
				Width = width;
				Height = height;
				Top = top;
				Left = left;
            }
		}
		public RegionDocument()
		{
			InitializeComponent();

			WeakReferenceMessenger.Default.Register<RegionDocument, JumpSegmentRegionLocation>(this, (r, m) => { MoveCamera(m); });
			WeakReferenceMessenger.Default.Register<RegionDocument, ExportRegionRequest>(this, (r, m) => { screenshot(m); });
		}
		private void MoveCamera ( JumpSegmentRegionLocation target)
        {
			if (_presenter.Region.ID == target.Region)
            {
				if (_presenter.WorldScreen is not null)
					_presenter.WorldScreen.CenterCameraOn(target.X, target.Y);
            }
        }
		private void screenshot (ExportRegionRequest message)
        {
			if (_presenter.Region != message.Region)
				return;
			_presenter.WorldScreen.GetPNG(message);

		}
	}
}