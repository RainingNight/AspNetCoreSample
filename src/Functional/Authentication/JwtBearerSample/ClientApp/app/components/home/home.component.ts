import { style } from '@angular/animations';
import { Component } from '@angular/core';
import { User } from 'oidc-client';
import { AuthService } from '../../services/auth.service';

@Component({
    selector: 'home',
    templateUrl: './home.component.html',
    styles: [
        `mat-card {
            margin: 20px;
        }`
    ]
})
export class HomeComponent {

    public user?: User;

    constructor(private authService: AuthService) {
        this.user = authService.currentUser;
    }
}
