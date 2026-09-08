using System;

namespace Devotion.SDK.Services.SaveSystem.Progress
{
    [Serializable]
    public class PurchasesProgress : BaseProgress
    {
        public System.Collections.Generic.List<string> GrantedTokens = new System.Collections.Generic.List<string>();
    }
}
