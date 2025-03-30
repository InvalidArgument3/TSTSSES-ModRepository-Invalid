using System.Collections.Generic;

namespace FactionsStruct
{
    
    public partial class FactionDefs
    {
        public List<FactionDefinition> defs = new List<FactionDefinition>();
        
        public FactionDefs()
        {
            // your faction definitions go here
            // defs.Add(YourFactionDefinition); add as many as you have in your mod
            defs.Add(EnceladusCorporationFaction);
        }
    }
    
}