import {Component, Inject, PLATFORM_ID} from '@angular/core';
import {isPlatformBrowser, isPlatformServer} from '@angular/common';
import {MatSnackBar} from '@angular/material';
import {DataSource} from '@angular/cdk/collections';
import {Observable} from 'rxjs/Observable';
import {AuthService} from '../../services/auth.service';
import 'rxjs/add/observable/of';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/catch';

interface WeatherForecast {
    dateFormatted : string;
    temperatureC : number;
    temperatureF : number;
    summary : string;
}

@Component({selector: 'fetchdata', templateUrl: './fetchdata.component.html'})
export class FetchDataComponent {
    public forecasts : ExampleDataSource;
    public displayedColumns = ['dateFormatted', 'temperatureC', 'temperatureF', 'summary'];

    constructor(http : AuthService, @Inject('BASE_URL')baseUrl : string, @Inject(PLATFORM_ID)private platformId : Object, private snackBar : MatSnackBar,) {
        if (isPlatformBrowser(this.platformId)) {
            this.forecasts = new ExampleDataSource(http, baseUrl, snackBar);
        }
    }
}

export class ExampleDataSource extends DataSource < any > {

    constructor(private http : AuthService, private baseUrl : string, private snackBar : MatSnackBar) {

        super();
    }

    connect() : Observable < WeatherForecast[] > {
        return this
            .http
            .AuthGet(this.baseUrl + 'api/SampleData/WeatherForecasts')
            .map(res => res.json())
            .catch((error : any) => {
                if (error.status == 401) {
                    this
                        .snackBar
                        .open("need sigin", error.statusText, {duration: 2000});
                    return Observable.throw(error.statusText);
                } else {
                    return Observable.throw("error");
                }
            });
    }

    disconnect() {}
}