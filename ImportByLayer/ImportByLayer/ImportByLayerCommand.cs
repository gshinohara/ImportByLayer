using Eto.Forms;
using Rhino;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ImportByLayer
{
    public class ImportByLayerCommand : Rhino.Commands.Command
    {
        public ImportByLayerCommand()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static ImportByLayerCommand Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "ImportByLayer";

        protected override Rhino.Commands.Result RunCommand(RhinoDoc doc, Rhino.Commands.RunMode mode)
        {
            string fileName = RhinoGet.GetFileName(GetFileNameMode.OpenRhinoOnly, default, "Select a Rhino file", RhinoEtoApp.MainWindow);
            File3dm file = File3dm.Read(fileName);
            File3dmLayerTable layers = file.AllLayers;

            List<Layer> layersSelected = new List<Layer>(); //Layer IDs you select in dialog.
            using (Dialog dialog = new Dialog())
            {
                dialog.Title = "Layer Selector";
                dialog.Padding = 10;
                dialog.AutoSize = true;
                dialog.Resizable = true;

                DynamicLayout layout = new DynamicLayout();
                List<CheckBox> checkBoxes = new List<CheckBox>();
                foreach (Layer layer in layers)
                {
                    int count = -1;
                    Guid lid = layer.Id;
                    while (lid != Guid.Empty)
                    {
                        lid = layers.FindId(lid).ParentLayerId;
                        count += 1;
                    }
                    CheckBox checkBox = new CheckBox() { Text = $"{new string('-', count)}{layer.Name}", Tag = layer };
                    layout.AddRow(checkBox);
                    checkBoxes.Add(checkBox);
                }
                layout.AddRow(null);

                Button ok = new Button() { Text = "OK" };
                ok.Click += (s, e) =>
                {
                    layersSelected.AddRange(checkBoxes.Where(c => (bool)c.Checked).Select(c => (Layer)c.Tag));
                    dialog.Close();
                };
                layout.AddRow(ok);

                dialog.Content = layout;

                dialog.ShowModal(RhinoEtoApp.MainWindow);
            }

            foreach(Layer layer in layersSelected)
            {
                foreach (File3dmObject obj in file.Objects.FindByLayer(layer))
                    doc.Objects.Add(obj.Geometry, obj.Attributes);
            }

            return Rhino.Commands.Result.Success;
        }
    }
}
