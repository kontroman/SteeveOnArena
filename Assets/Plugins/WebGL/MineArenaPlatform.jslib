mergeInto(LibraryManager.library, {
  MineArena_Bind: function (name) {
    window.MineArenaPlatform.bind(UTF8ToString(name));
  },
  MineArena_Request: function (json) {
    window.MineArenaPlatform.request(UTF8ToString(json));
  }
});
