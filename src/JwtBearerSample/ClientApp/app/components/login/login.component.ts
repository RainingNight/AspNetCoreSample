import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material';

import { AuthService } from '../../services/auth.service';

@Component({ selector: 'login', templateUrl: './login.component.html', styles: [`.full-width {width: 100%;}`] })
export class LoginComponent {
    public userName: string;
    public password: string;
    public loading = false;

    returnUrl: string;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private snackBar: MatSnackBar,
        private authService: AuthService) { }

    ngOnInit() {
        this.returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/';
    }

    login() {
        this.loading = true;
        this.authService.localLogin(this.userName, this.password)
            .subscribe(data => {
                this
                    .router
                    .navigate([this.returnUrl]);
            }, error => {
                this
                    .snackBar
                    .open("Username or password is incorrect", 'Error', { duration: 2000 });
                this.loading = false;
            });
    }
}