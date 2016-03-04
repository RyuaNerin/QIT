// 아즈레아 TiX 런처
// modified by @SasarinoMARi
// Last Update: 2016-03-02
// see more info : http://usagination.com

// Ctrl + Shift + C : 화면 캡처 후 트윗
// Ctrl + Shift + V : 화면 캡처 후 선택된 트윗에 멘션

Array.prototype.contains = function(element) {
	for (var i = 0; i < this.length; i++)
		if (this[i].toLowerCase() == element.toLowerCase())
			return true;
	return false;
}

FileSystem.privateStore.write('location.dat', System.applicationPath.replace(/[^(.)^(\\)]+(.)exe/, ''), 3);
function run(exe, arg) {
    var path = FileSystem.privateStore.read('location.dat') + exe;
    System.launchApplication(path, arg, 1);
}
System.addContextMenuHandler('Quicx 실행', 0, function (id) {
    run('TiX/TiX.exe', '');
});
System.addKeyBindingHandler('C'.charCodeAt(0), 3, function (id) {
    run('TiX/TiX.exe', 'Stasis');
});
System.addKeyBindingHandler('V'.charCodeAt(0), 3, function (id) {
    if (id == undefined) return;
    
	var status = TwitterService.status.get(id);
	if (!status) return;

	var status_users = [];
	TwitterService.status.getUsers(id, status_users);

	var me = TwitterService.currentUser.screen_name.toLowerCase();
	var new_users = new Array();
	
	if (status.user.screen_name.toLowerCase() != me)
		new_users.push(status.user.screen_name);
	
	for (var i = 0; i < status_users.length; i++)
		if (status_users[i].toLowerCase() != me)
			if (!new_users.contains(status_users[i]))
				new_users.push(status_users[i]);

    var str = '';
	if (new_users.length > 0)
		str = '@' +  new_users.join(' @') + ' ';
	else
		str = '';
	TextArea.in_reply_to_status_id = status_id;
    
    run('TiX/TiX.exe', 'Stasis "' + str + '" "' + id + '"');
});
