import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Maincontent } from './maincontent';

describe('Maincontent', () => {
  let component: Maincontent;
  let fixture: ComponentFixture<Maincontent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Maincontent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Maincontent);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
