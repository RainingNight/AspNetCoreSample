import {NgModule} from '@angular/core';
import {CommonModule} from '@angular/common';
import {FormsModule} from '@angular/forms';
import {HttpModule} from '@angular/http';
import {RouterModule} from '@angular/router';
import {
    MatAutocompleteModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatCheckboxModule,
    MatChipsModule,
    MatDatepickerModule,
    MatDialogModule,
    MatExpansionModule,
    MatGridListModule,
    MatIconModule,
    MatInputModule,
    MatListModule,
    MatMenuModule,
    MatNativeDateModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatRadioModule,
    MatRippleModule,
    MatSelectModule,
    MatSidenavModule,
    MatSliderModule,
    MatSlideToggleModule,
    MatSnackBarModule,
    MatSortModule,
    MatTableModule,
    MatTabsModule,
    MatToolbarModule,
    MatTooltipModule,
    MatStepperModule
} from '@angular/material';
import {CdkTableModule} from '@angular/cdk/table';

import {AuthModule} from './app.auth.module';

import {AppComponent} from './components/app/app.component';
import {NavMenuComponent} from './components/navmenu/navmenu.component';
import {HomeComponent} from './components/home/home.component';
import {LoginComponent} from './components/login/login.component';
import {FetchDataComponent} from './components/fetchdata/fetchdata.component';

import {AuthenticationService} from './services/authentication.service';

@NgModule({
    exports: [
        CdkTableModule,
        MatAutocompleteModule,
        MatButtonModule,
        MatButtonToggleModule,
        MatCardModule,
        MatCheckboxModule,
        MatChipsModule,
        MatStepperModule,
        MatDatepickerModule,
        MatDialogModule,
        MatExpansionModule,
        MatGridListModule,
        MatIconModule,
        MatInputModule,
        MatListModule,
        MatMenuModule,
        MatNativeDateModule,
        MatPaginatorModule,
        MatProgressBarModule,
        MatProgressSpinnerModule,
        MatRadioModule,
        MatRippleModule,
        MatSelectModule,
        MatSidenavModule,
        MatSliderModule,
        MatSlideToggleModule,
        MatSnackBarModule,
        MatSortModule,
        MatTableModule,
        MatTabsModule,
        MatToolbarModule,
        MatTooltipModule
    ]
})
export class PlunkerMaterialModule {}

@NgModule({
    declarations: [
        AppComponent, NavMenuComponent, HomeComponent, LoginComponent, FetchDataComponent
    ],
    providers: [
        AuthModule, AuthenticationService
    ],
    imports: [
        CommonModule,
        HttpModule,
        FormsModule,
        PlunkerMaterialModule,
        AuthModule,
        RouterModule.forRoot([
            {
                path: '',
                redirectTo: 'home',
                pathMatch: 'full'
            }, {
                path: 'home',
                component: HomeComponent
            }, {
                path: 'login',
                component: LoginComponent
            }, {
                path: 'fetch-data',
                component: FetchDataComponent
            }, {
                path: '**',
                redirectTo: 'home'
            }
        ])
    ]
})
export class AppModuleShared {}