import {Component} from '@angular/core';
import {Router} from '@angular/router';
import {tokenNotExpired} from 'angular2-jwt';
import {AuthenticationService} from '../../services/authentication.service';

@Component({selector: 'nav-menu', templateUrl: './navmenu.component.html', styles: ['spacer { flex: 1 1 auto; }']})
export class NavMenuComponent {
    constructor(private router : Router, private authenticationService : AuthenticationService) {}

    loggedIn() {
        return tokenNotExpired();
    }

    logout() {
        this
            .authenticationService
            .logout();
        this
            .router
            .navigate(["/"]);
    }

}
