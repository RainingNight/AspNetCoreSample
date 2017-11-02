import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { GlobalEventsManager } from '../../services/global.events.manager'

@Component({
    selector: 'nav-menu',
    templateUrl: './navmenu.component.html',
    styles: ['spacer { flex: 1 1 auto; }']
})
export class NavMenuComponent {
    public _loggedIn: boolean = false;

    constructor(
        private router: Router,
        private authService: AuthService,
        private globalEventsManager: GlobalEventsManager) {
        globalEventsManager.showNavBarEmitter.subscribe((mode) => {
            // mode will be null the first time it is created, so you need to igonore it when null
            if (mode !== null) {
                console.log("Global Event, sent: " + mode);
                this._loggedIn = mode;
            }
        });
    }

    remoteLogin() {
        this.authService.remoteLogin();
    }

    logout() {
        this.authService.logout();
        this.router.navigate(["/"]);
    }

    remoteLogout() {
        this.authService.remoteLogout();
    }

}
