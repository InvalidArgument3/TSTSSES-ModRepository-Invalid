using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;
using ServerMod;
using MIG.Shared.SE;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;




namespace MIG.SpecCores
{   

    [MyTextSurfaceScriptAttribute("CoreInfoLcd", "Core Info Lcd")]
    public class CoreInfoLcd : MyTSSCommon
    {

        private IMyTextSurface lcd;  
        private IMyCubeBlock ts_block;
        private RectangleF viewport;        
        private IMyCubeGrid grid;
        private StringBuilder RowInfo = new StringBuilder();
        private Color TextColor = Color.White;


        public CoreInfoLcd(IMyTextSurface lcd_surface, IMyCubeBlock ts_block, Vector2 size) : base(lcd_surface, ts_block, size)
        {
            this.ts_block = ts_block;
            lcd = lcd_surface;
        }        

        public override void Run()
        {
            try
            {
                grid = ts_block.CubeGrid;
                Update();
            }
            catch (Exception ex)
            {
                if (ts_block is IMyTextSurfaceProvider)
                {
                    (ts_block as IMyTextSurfaceProvider).GetSurface(0).WriteText("ERROR DESCRIPTION:\n" + ex);
                }
            }            
        }

        private void Update()
        {
            if (SearchSpecCore())
            {                
                viewport = new RectangleF((lcd.TextureSize - lcd.SurfaceSize) / 2f, lcd.SurfaceSize);                 
                TextColor = lcd.ScriptForegroundColor;
                Draw();
            }
        }

        private bool SearchSpecCore()
        {
            bool scready;
            try
            {
                var core = (ISpecBlock)Hooks.GetMainSpecCore(grid);
                RowInfo.Clear();
                core.BlockOnAppendingCustomInfo(core.block, RowInfo);                
                scready = true;
            }
            catch (Exception e)
            {
                RowInfo.Clear();
                RowInfo.AppendLine("SpecCore not found!");
                scready = false;
            }
            return scready;
        }
        
        private void Draw()
        {
            var frame = lcd.DrawFrame();
            var text = PreparedText();
            var txtboxsize = lcd.MeasureStringInPixels(RowInfo, "Debug", 1f);

            var minside = Math.Min(viewport.Size.X, viewport.Size.Y);
            var textscale = MathHelper.Clamp(minside / txtboxsize.Y, 0.4f, 0.75f);

            var charpattern = "[";
            var patterstring = new StringBuilder();
            patterstring.Append(charpattern);
            var charsize = lcd.MeasureStringInPixels(patterstring, "Debug", textscale);  
            
            var position = new Vector2(viewport.X + 10f, viewport.Y + charsize.Y );


            for (int i = 0; i < text.Length; i++)
            {
                var offset = new Vector2(0, charsize.Y * i);

                var textline = CreateSprite(TextAlignment.LEFT, new Vector2(), text[i], position + offset, TextColor, textscale);
                frame.Add(textline);
            }

            frame.Dispose();
           
        }

        
        private string[] PreparedText()
        {           
            var sb = new StringBuilder();
            string[] tmptext = RowInfo.ToString().Split(new[] { "\n" }, StringSplitOptions.None).ToArray();
            for(int i =0; i < tmptext.Length; i++)
            {
                var line = tmptext[i].Replace("[/Color]", "");
                int startindex = line.IndexOf("]") + 1;

                sb.AppendLine(line.Substring(startindex));

            }
            
            return sb.ToString().Split(new[] { "\n" }, StringSplitOptions.None).Skip(1).ToArray(); ;
        }
           

        private MySprite CreateSprite(TextAlignment textAlignment, Vector2 size, string text_or_type, Vector2 position, Color color, float rotation_or_scale)
        {
             return new MySprite(SpriteType.TEXT, text_or_type, position, size, color, "Debug", textAlignment, rotation_or_scale);            
   
        }


        public override ScriptUpdate NeedsUpdate => ScriptUpdate.Update10;

    }
}