using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Xml.Linq;
using CommonServiceLocator;
using DigitalRune;
using DigitalRune.Game;
using DigitalRune.Game.UI;
using DigitalRune.Game.UI.Controls;
using DigitalRune.Mathematics.Algebra;
using Kesmai.WorldForge.Editor;
using Kesmai.WorldForge.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using HorizontalAlignment = DigitalRune.Game.UI.HorizontalAlignment;
using VerticalAlignment = DigitalRune.Game.UI.VerticalAlignment;
using Window = DigitalRune.Game.UI.Controls.Window;

namespace Kesmai.WorldForge.Windows
{
	public class ReplaceWindow : Window
	{
		private WorldGraphicsScreen _screen;
		private SegmentRegion _region;
		private ApplicationPresenter _presenter;
		private TextBox _replaceIDFrom;
		private TextBox _replaceIDTo;
		private DropDownButton _replaceComponentFrom;
		private TextBox _replaceComponentFromStatic;
		private DropDownButton _replaceComponentTo;
		private List<String> _typeChoices = new List<String>();

		public ReplaceWindow(SegmentRegion region, WorldGraphicsScreen screen)
		{
			_screen = screen;
			_region = region;
			_presenter = ServiceLocator.Current.GetInstance<ApplicationPresenter>();
			_typeChoices = GetComponentTypes().ToList();
			_typeChoices.Sort();
			Title = "Replace";
		}

		protected override void OnLoad()
		{
			base.OnLoad();

	
			// Can I build this form using XAML? It'd probably look nicer if I can. Me != UX Designer
			var tabControl = new TabControl();
			var replaceID = new TabItem()
			{
				Name = "ReplaceID",
				Content = new TextBlock() { Text = "Replace a Static ID across selection" }
			};

			var replaceIDPage = new StackPanel() { Orientation = Orientation.Vertical, Padding = new Vector4F(2) };
			var replaceIDButton = new Button() { Name = "Replace", Content = new TextBlock() { Text = "Replace" }, Margin = new Vector4F(4) };
				replaceIDButton.Click += ReplaceStaticIDs;
			_replaceIDFrom = new TextBox() { Width = 300, Margin = new Vector4F(4) };
			_replaceIDTo = new TextBox() { Width = 300, Margin = new Vector4F(4) };
			
			replaceIDPage.Children.Add(new TextBlock() { Text = "Static ID to find:"});
			replaceIDPage.Children.Add(_replaceIDFrom);
			replaceIDPage.Children.Add(new TextBlock() { Text = "Static ID to replace with:"});
			replaceIDPage.Children.Add(_replaceIDTo);
			replaceIDPage.Children.Add(replaceIDButton);		

			replaceID.TabPage = replaceIDPage;

			var replaceComponent = new TabItem()
			{
				Name = "ReplaceComponent",
				Content = new TextBlock() { Text = "Replace a Component across selection" }
			};

			var replaceComponentPage = new StackPanel() { Orientation = Orientation.Vertical, Padding = new Vector4F(2) };
			var replaceComponentButton = new Button() { Name = "Replace", Content = new TextBlock() { Text = "Replace" }, Margin = new Vector4F(4) };
			replaceComponentButton.Click += ReplaceComponents;
			_replaceComponentFrom = new DropDownButton() {  Width = 300, MaxDropDownHeight = 250, Margin = new Vector4F(4) };
			_replaceComponentFromStatic = new TextBox() { Width = 300, Margin = new Vector4F(4) };
			_replaceComponentTo = new DropDownButton() { Width = 300, MaxDropDownHeight = 200, Margin = new Vector4F(4) };
			_replaceComponentFrom.Items.AddRange(_typeChoices);
			_replaceComponentTo.Items.AddRange(_typeChoices);

			replaceComponentPage.Children.Add(new TextBlock() { Text = "Type of component to find:" });
			replaceComponentPage.Children.Add(_replaceComponentFrom);
			replaceComponentPage.Children.Add(new TextBlock() { Text = "Static ID filter of component to replace:" });
			replaceComponentPage.Children.Add(_replaceComponentFromStatic);
			replaceComponentPage.Children.Add(new TextBlock() { Text = "Type of component to replace with:" });
			replaceComponentPage.Children.Add(_replaceComponentTo);
			replaceComponentPage.Children.Add(new TextBlock() { Text = "Note, replacing WallComponents or DoorComponents with\nother types will lose data.\nReplacing components with Wall or Door components will not set\nDestroyed, Opened or other Statics." });
			replaceComponentPage.Children.Add(replaceComponentButton);

			replaceComponent.TabPage = replaceComponentPage;

			tabControl.Items.Add(replaceID);
			tabControl.Items.Add(replaceComponent);
			tabControl.SelectedIndex = 0;

			Content = tabControl;
		}

		private IEnumerable<String> GetComponentTypes()
        {
			return Assembly.GetAssembly(typeof(TerrainComponent)).GetTypes() // get all the types
				.Where(t=>t.IsSubclassOf(typeof(TerrainComponent))) // get the ones derived from TerrainComponent
				.Select(t => t.ToString().Split('.').Last()) // get only the last token of the class name
				.Where(type => !(type.Contains("teleport",StringComparison.InvariantCultureIgnoreCase)|| type.Contains("egress", StringComparison.InvariantCultureIgnoreCase))); // ignore the teleporters - it seems unlikely that anyone would want to bulk change to or from these
        }

		protected override void OnHandleInput(InputContext context)
		{
			if (!IsVisible)
				return;

			base.OnHandleInput(context);

			var inputService = InputService;

			if (inputService == null)
				return;

			if (!inputService.IsKeyboardHandled)
			{
				if (inputService.IsReleased(Keys.Escape))
				{
					Close();
				}
			}

			if (IsActive)
				inputService.IsKeyboardHandled = true;
		}

		private void ReplaceStaticIDs(object sender, EventArgs eventArgs)
        {
			int from;
			int to;
			if (!int.TryParse(_replaceIDFrom.Text, out from))
				return;
			if (!int.TryParse(_replaceIDTo.Text, out to))
				return;
			
			foreach (Rectangle selection in _presenter.Selection)
            {
				var tiles = _region.GetTiles(t=>selection.Contains(t.X, t.Y));
				foreach (var tile in tiles)
                {
					var components = tile.GetComponents<TerrainComponent>();
					foreach (var component in components)
                    {
						switch (component)
                        {
							case StaticComponent S:
								if (S.Static == from)
									S.Static = to;
								break;
							case FloorComponent F:
								if (F.Ground == from)
									F.Ground = to;
								break;
							case TreeComponent Tree:
								if (Tree.Tree == from)
									Tree.Tree = to;
								break;
							case RuinsComponent Ruins:
								if (Ruins.Ruins == from)
									Ruins.Ruins = to;
								break;
							case AltarComponent A:
								if (A.Altar == from)
									A.Altar = to;
								break;
							case CounterComponent Counter:
								if (Counter.Counter == from)
									Counter.Counter = to;
								break;
							case DoorComponent Door:
								if (Door.OpenId == from)
									Door.OpenId = to;
								if (Door.ClosedId == from)
									Door.ClosedId = to;
								if (Door.DestroyedId == from)
									Door.DestroyedId = to;
								break;
							case ObstructionComponent Obstruction:
								if (Obstruction.Obstruction == from)
									Obstruction.Obstruction = to;
								break;
							case WallComponent Wall:
								if (Wall.Wall == from)
									Wall.Wall = to;
								if (Wall.Ruins == from)
									Wall.Ruins = to;
								if (Wall.Destroyed == from)	
									Wall.Destroyed = to;
								break;
                        }
                    }
					tile.UpdateTerrain();
                }
            }

			_screen.InvalidateRender();
			this.Close();
		}

		private void ReplaceComponents(object sender, EventArgs eventArgs)
		{
			int filter;

			if (_replaceComponentFrom.SelectedIndex == -1 || _replaceComponentTo.SelectedIndex == -1)
				return;

			String fromType = _typeChoices.ElementAt(_replaceComponentFrom.SelectedIndex);
			String toType = _typeChoices.ElementAt(_replaceComponentTo.SelectedIndex);
			if (!int.TryParse(_replaceComponentFromStatic.Text, out filter))
				filter = -1;

			foreach (Rectangle selection in _presenter.Selection)
			{
				var tiles = _region.GetTiles(t => selection.Contains(t.X, t.Y));
				foreach (var tile in tiles)
				{
					var components = tile.GetComponents<TerrainComponent>().Where(c => c.GetType().ToString().EndsWith(fromType));
					foreach (var component in components)
					{
						int currentStatic;
						switch (fromType)
                        {
							case "FloorComponent":
							case "WaterComponent":
							case "PoisonedWaterComponent":
							case "IceComponent":
								currentStatic = (component as FloorComponent).Ground;
								break;
							case "RopeComponent":
							case "ShaftComponent":
							case "StaircaseComponent":
							case "SkyComponent":
								currentStatic = (component as ActiveTeleporter).TeleporterId;
								break;
							case "StaticComponent":
								currentStatic = (component as StaticComponent).Static;
								break;
							case "WallComponent":
								currentStatic = (component as WallComponent).Wall;
								break;
							case "DoorComponent":
								currentStatic = (component as DoorComponent).ClosedId;
								break;
							case "TreeComponent":
								currentStatic = (component as TreeComponent).Tree;
								break;
							case "AltarComponent":
								currentStatic = (component as AltarComponent).Altar;
								break;
							case "CounterComponent":
								currentStatic = (component as CounterComponent).Counter;
								break;
							case "RuinsComponent":
								currentStatic = (component as RuinsComponent).Ruins;
								break;
							case "ObstructionComponent":
								currentStatic = (component as ObstructionComponent).Obstruction;
								break;
							default: // fire, darkness, web, whirlwind, trash
								currentStatic = -1;
								break;
                        }

						//skip this component if none of these are true: we don't have a filter, this type doesn't care about statics, or the current static matches the filter
						if (!(filter == -1 || currentStatic == -1 || currentStatic == filter))
							continue;

						tile.RemoveComponent(component);
						
						Type componentType;
						try
						{
							var componentTypename = $"Kesmai.WorldForge.Models.{toType}";
							componentType = Assembly.GetExecutingAssembly().GetType(componentTypename, false);
						}
						catch { continue; }

						TerrainComponent newComponent;
						switch (toType)
                        {
							case "WallComponent":
								
								 newComponent = Activator.CreateInstance(componentType,currentStatic, 0, 0, false) as TerrainComponent; // walls take 3 static IDs: wall, destroyed and ruins; and one bool: isIndestructible
								break;
							case "DoorComponent":
								newComponent = Activator.CreateInstance(componentType, currentStatic, 0, 0, 0, false, false) as TerrainComponent; // Doors take 4 static IDs: closed, opened, secret, destroyed; and two bool: isSecret, isOpened
								break;
							case "FloorComponent":
								newComponent = Activator.CreateInstance(componentType, currentStatic, 1 ) as TerrainComponent; // movement cost as the extra param
								break;
							case "WaterComponent":
							case "PoisonedWaterComponent":
								newComponent = Activator.CreateInstance(componentType, currentStatic, 3 ) as TerrainComponent; // depth as the extra param
								break;
							case "Darkness":
							case "Fire":
							case "Web":
								newComponent = Activator.CreateInstance(componentType, args: true) as TerrainComponent; // canDispell as the extra param
								break;
							case "ObstructionComponent":
								newComponent = Activator.CreateInstance(componentType, currentStatic, false ) as TerrainComponent; // blocksVision as the extra param
								break;
							case "Whirlwind":
								newComponent = Activator.CreateInstance(componentType, 3, true) as TerrainComponent; // damage and canDispell as the extra params
								break;
							case "CounterComponent":
								newComponent = Activator.CreateInstance(componentType, currentStatic, Direction.None) as TerrainComponent; // Counters have an AccessDirection
								break;
							case "SkyComponent":
								newComponent = Activator.CreateInstance(componentType, currentStatic, 0, 0, 0) as TerrainComponent; // Teleporters get a destination
								break;
							case "RopeComponent":
								newComponent = Activator.CreateInstance(componentType, currentStatic, 0, 0, 0, false) as TerrainComponent; // Ropes have a isSecret flag
								break;
							case "ShaftComponent":
								newComponent = Activator.CreateInstance(componentType, currentStatic, 0, 0, 0, 0) as TerrainComponent; // Shafts have a slipchance
								break;
							case "StaircaseComponent":
								newComponent = Activator.CreateInstance(componentType, currentStatic, 0, 0, 0, false) as TerrainComponent; // Staircases have a 'descends' flag that doesn't seem to be used
								break;
							default:
								newComponent = Activator.CreateInstance(componentType,currentStatic) as TerrainComponent; // all other components (altars, static, trees...) just take a static
								break;
						}
						if (newComponent is TerrainComponent)
							tile.Components.Add(newComponent);
					}
					tile.UpdateTerrain();
				}
			}

			_screen.InvalidateRender();
			this.Close();
		}

	}
}