import {Inject, Injectable} from '@angular/core';
import {Http, Headers, Response} from '@angular/http';
import {Observable} from 'rxjs/Observable';
import 'rxjs/add/operator/map'

@Injectable()
export class AuthenticationService {
    constructor(private http : Http, @Inject('BASE_URL')private baseUrl : string) {}

    login(username : string, password : string) {
        return this
            .http
            .post(this.baseUrl + 'api/oauth/authenticate', {
                username: username,
                password: password
            })
            .map((response : Response) => {
                let user = response.json();
                if (user && user.token) {
                    localStorage.setItem('currentUser', JSON.stringify(user));
                    localStorage.setItem('token', user.token);
                }
            });
    }

    logout() {
        localStorage.removeItem('currentUser');
        localStorage.removeItem('token');
    }
}