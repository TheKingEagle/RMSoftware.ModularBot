
function ShowAlert(type, message, hidebn = false, autohide = false, duration = 4000, bntx = 'Reload', bnlk = 'javascript:window.location.reload()') {
    supported_types = ['info', 'warning', 'error', 'success'];
    let lowerCaseArray = supported_types.map(item => item.toLowerCase());
    let lowerCaseSearchString = type.toLowerCase();

    if (!lowerCaseArray.includes(lowerCaseSearchString)) {
        console.log(`defaulting to info`);
        type = "info";
    }
    document.querySelector("#main-alert").classList.remove('hide-action');
    document.querySelector("#main-alert").classList.remove('hidden');
    document.querySelector("#main-alert").classList.remove('info');
    document.querySelector("#main-alert").classList.remove('warning');
    document.querySelector("#main-alert").classList.remove('error');
    document.querySelector("#main-alert").classList.remove('success');
    document.querySelector("#main-alert").classList.add(type);
    document.querySelector("#main-alert .alert-text .alert").innerHTML = message;
    document.querySelector("#main-alert .action").innerHTML = "<a href='" + bnlk + "'>" + bntx + "</a>";
    if (hidebn) {
        document.querySelector("#main-alert").classList.add('hide-action');
    }
    if (autohide) {
        setTimeout(() => {
            document.querySelector("#main-alert").classList.add('hidden');
        }, duration)
    }
}

function ConnectEventServer() {
    let _error = false;
    socket.on('connect', () => {
        console.log('Connected to the server socket.');

        if (_error) {
            ShowAlert("success", "Reconnected to event server", true, true, 2000);
            _error = false;
        }
    });
    socket.on('connect_error', (error) => {
        console.error('Connection error:', error.message);
        ShowAlert("error", "Failed to connect to event server.", true, false, 2000);

        _error = true;
    });

}

addEventListener("DOMContentLoaded", (event) => {
    ConnectEventServer();
});