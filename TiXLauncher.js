// ����� TiX ��ó
// modified by @SasarinoMARi
// Last Update: 2016-03-02
// see more info : http://usagination.com

// Ctrl + Shift + C : ȭ�� ĸó �� Ʈ��
// Ctrl + Shift + V : ȭ�� ĸó �� ���õ� Ʈ���� ���

FileSystem.privateStore.write('location.dat', System.applicationPath.replace(/[^(.)^(\\)]+(.)exe/, ''), 3);
function run(exe, arg) {
    var path = FileSystem.privateStore.read('location.dat') + exe;
    System.launchApplication(path, arg, 1);
}
System.addContextMenuHandler('Quicx ����', 0, function (id) {
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