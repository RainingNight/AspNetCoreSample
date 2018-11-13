import { Component } from '@angular/core';

@Component({
    selector: 'app',
    template: `
        <nav-menu></nav-menu>
        <div class="app-content">
            <router-outlet></router-outlet>
        </div>
    `,
    styles: ['.app-content { padding: 20px; }']
})
export class AppComponent {
}
