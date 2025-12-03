import { Routes } from '@angular/router';

import { LoginComponent } from './auth/login.component';
import { authGuard } from './core/auth.guard';
import { ErpLandingComponent } from './main-layout/erp-landing.component';
import { HomeContentComponent } from './main-layout/home-content.component';
import { ModulesContentComponent } from './main-layout/modules-content.component';
import { PurchasingModuleComponent } from './purchasing/purchasing-module.component';
import { StockMonitoringComponent } from './purchasing/stock-monitoring.component';
import { PurchasingPlaceholderComponent } from './purchasing/placeholder-panel.component';
import { RawDataTableComponent } from './purchasing/raw-data-table.component';

export const routes: Routes = [
  { path: '', redirectTo: 'home', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: ErpLandingComponent,
    children: [
      { path: 'home', component: HomeContentComponent },
      { path: 'modules', component: ModulesContentComponent, canActivate: [authGuard] },
      {
        path: 'purchasing',
        component: PurchasingModuleComponent,
        canActivate: [authGuard],
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'warehouse' },
          {
            path: 'transactions',
            component: PurchasingPlaceholderComponent,
            data: { title: 'Satın Alma İşlemleri' }
          },
          {
            path: 'warehouse',
            component: StockMonitoringComponent
          },
          {
            path: 'vendor',
            component: PurchasingPlaceholderComponent,
            data: { title: 'Tedarikçi İşlemleri' }
          },
          {
            path: 'logistics',
            component: PurchasingPlaceholderComponent,
            data: { title: 'Lojistik İşlemleri' }
          },
          {
            path: 'definitions',
            component: PurchasingPlaceholderComponent,
            data: { title: 'Tanımlar' }
          },
          {
            path: 'data/:tableName',
            component: RawDataTableComponent
          }
        ]
      }
    ]
  },
  { path: '**', redirectTo: 'home' }
];
