import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MenuComponent } from './menu/menu';
import { Maincontent } from './maincontent/maincontent';


@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MenuComponent, Maincontent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('FlightFrontend');
}
