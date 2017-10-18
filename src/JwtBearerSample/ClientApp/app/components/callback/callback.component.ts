import {Component} from '@angular/core';
import { AuthService } from '../../services/auth.service'

@Component({
    selector: 'callback',
    template: `<div></div>`
})
export class CallbackComponent {
    constructor (private _authService: AuthService){
        _authService.endSigninMainWindow();
    }
}