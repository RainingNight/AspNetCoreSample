import {NgModule} from '@angular/core';
import {BrowserAnimationsModule} from '@angular/platform-browser/animations';
import {AppModuleShared} from './app.module.shared';
import {AppComponent} from './components/app/app.component';

@NgModule({
    bootstrap: [AppComponent],
    imports: [
        BrowserAnimationsModule, AppModuleShared
    ],
    providers: [
        {
            provide: 'BASE_URL',
            useFactory: getBaseUrl
        }
    ]
})
export class AppModule {}

export function getBaseUrl() {
    return document.getElementsByTagName('base')[0].href;
}
