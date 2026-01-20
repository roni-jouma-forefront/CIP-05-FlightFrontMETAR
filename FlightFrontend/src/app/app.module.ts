import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { RouterModule } from '@angular/router';
import { MenuComponent } from './menu/menu';
import { HttpClientModule, provideHttpClient } from '@angular/common/http';

@NgModule({
  declarations: [],
  imports: [
    BrowserModule,
    RouterModule,
    MenuComponent,
    HttpClientModule
  ],
  exports: [],
  providers: [ provideHttpClient(),]
})
export class AppModule {}
