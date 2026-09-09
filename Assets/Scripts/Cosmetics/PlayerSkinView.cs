using UnityEngine;
using MineArena.Networking;

namespace MineArena.Cosmetics
{
    [DisallowMultipleComponent]
    public sealed class PlayerSkinView : MonoBehaviour
    {
        [SerializeField] private Renderer body;
        private MaterialPropertyBlock block;
        private Texture original;
        private string applied;
        private bool remote;
        private void Awake()
        {
            if (body != null) original = body.sharedMaterial.mainTexture;
            block = new MaterialPropertyBlock();
        }
        private void OnEnable() { SkinService.Changed += Refresh; Refresh(); }
        private void Start() => Refresh();
        private void OnDisable() => SkinService.Changed -= Refresh;
        private void Update()
        {
            // Also handles a player created before asynchronous save loading finishes.
            if (!remote && applied != (SkinService.Progress?.Equipped ?? "default")) Refresh();
        }
        private void Refresh()
        {
            var network = GetComponent<NetworkPlayerView>();
            if (remote || (network != null && !network.IsLocalPlayer)) return;
            Apply(SkinService.Progress?.Equipped ?? "default");
        }
        public void ApplyRemote(string id) { remote = true; Apply(id); }
        public void Apply(string id)
        {
            if (body == null || applied == id) return;
            var skin = SkinCatalog.Load()?.Find(id);
            body.GetPropertyBlock(block ??= new MaterialPropertyBlock());
            var texture = skin?.Texture != null ? skin.Texture : original;
            block.SetTexture("_MainTex", texture);
            block.SetTexture("_BaseMap", texture);
            body.SetPropertyBlock(block);
            applied = id;
        }
    }
}
