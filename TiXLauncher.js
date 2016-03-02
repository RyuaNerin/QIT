// ¾ÆÁî·¹¾Æ TiX ·±Ã³
// modified by @SasarinoMARi
// Last Update: 2016-03-02
// see more info : http://usagination.com

// Ctrl + Shift + C : È­¸é Ä¸Ã³ ÈÄ Æ®À­
// Ctrl + Shift + V : È­¸é Ä¸Ã³ ÈÄ ¼±ÅÃµÈ Æ®À­¿¡ ¸à¼Ç

FileSystem.privateStore.write('location.dat', System.applicationPath.replace(/[^(.)^(\\)]+(.)exe/, ''), 3);
function run(exe, arg) {
    var path = FileSystem.privateStore.read('location.dat') + exe;
    System.launchApplication(path, arg, 1);
}
System.addContextMenuHandler('Quicx ½ÇÇà', 0, function (id) {
    run('TiX/TiX.exe', '');
});
System.addKeyBindingHandler('C'.charCodeAt(0), 3, function (id) {
    run('TiX/TiX.exe', 'Stasis');
});
System.addKeyBindingHandler('V'.charCodeAt(0), 3, function (id) {
    if (id == undefined) return;
    var username = TwitterService.status.get(id).user.screen_name;
    run('TiX/TiX.exe', 'Stasis ' + username + ' ' + id);
});