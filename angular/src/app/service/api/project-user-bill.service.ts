import { AddSubInvoiceModel } from './../../modules/pm-management/list-project/list-project-detail/project-bill/add-sub-invoice-dialog/add-sub-invoice-dialog.component';
import { SubInvoice } from './../model/bill-info.model';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';

import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ParentInvoice } from '../model/bill-info.model';
import { ApiResponse } from '../model/api-response.dto';
import {UpdateInvoiceDto} from '../model/updateInvoice.dto'
import { ProjectInvoiceSettingDto } from '@app/service/model/project-invoice-setting.dto';
@Injectable({
  providedIn: 'root'
})
export class ProjectUserBillService extends BaseApiService {
  changeUrl() {
    return 'ProjectUserBill'
  }
  constructor(http: HttpClient) {
    super(http);
  }
  getAllUserBill(input): Observable<any> {
    return this.http.post<any>(this.rootUrl + `/GetAllByProject`,input);
  }
  GetProjectUserBillById(projectUserBillId: number): Observable<any> {
    return this.http.get<any>(this.rootUrl + `/GetProjectUserBillById?projectUserBillId=${projectUserBillId}`);
  }
  public GetAllUser(projectId, userId, onlyStaff: boolean, isIncludedUserInPUB: boolean, isFake?: any): Observable<any> {
    if (isFake) {
      return this.http.get<any>(
        this.rootUrl +
          `/GetAllUser?onlyStaff=${onlyStaff}&projectId=${projectId}&currentUserId=${userId}&isFake=${isFake}&isIncludedUserInPUB=${isIncludedUserInPUB}`
      );
    } else {
      return this.http.get<any>(
        this.rootUrl + `/GetAllUser?onlyStaff=${onlyStaff}`
      );
    }
  }
  deleteUserBill(id: number): Observable<any> {
    return this.http.delete<any>(this.rootUrl + `/Delete?projectUserBillId=${id}`)
  }
  getRate(id: number): Observable<any> {
    return this.http.get<any>(this.rootUrl + `/GetRate?projectId=${id}`);
  }
  getLastInvoiceNumber(id: number): Observable<any> {
    return this.http.get<any>(this.rootUrl + `/GetLastInvoiceNumber?projectId=${id}`);
  }
  updateLastInvoiceNumber(item: any): Observable<any> {
    return this.http.put<any>(this.rootUrl + '/UpdateLastInvoiceNumber', item);
  }
  getDiscount(id: number): Observable<any> {
    return this.http.get<any>(this.rootUrl + `/GetDiscount?projectId=${id}`);
  }
  updateDiscount(item: any): Observable<any> {
    return this.http.put<any>(this.rootUrl + '/UpdateDiscount', item);
  }
  updateNote(item: any): Observable<any> {
    return this.http.put<any>(this.rootUrl + '/UpdateNote', item);
  }
  GetAllResource(): Observable<any>{
    return this.http.get<any>(this.rootUrl + '/GetAllResource');
  }
  LinkUserToBillAccount(input): Observable<any>{
    return this.http.post<any>(this.rootUrl + '/LinkUserToBillAccount',input);
  }
  RemoveLinkedResource(input): Observable<any>{
    return this.http.post<any>(this.rootUrl + '/RemoveLinkedResource',input);
  }
  LinkOneProjectUserBillAccount(input): Observable<any>{
    return this.http.post<any>(this.rootUrl + '/LinkOneProjectUserBillAccount',input);
  }
  //#region Integrate Finfast
  getParentInvoice(projectId: number): Observable<ApiResponse<ParentInvoice>> {
    return this.http.get<ApiResponse<ParentInvoice>>(this.rootUrl + '/GetParentInvoiceByProject?projectId=' + projectId);
  }
  getAllProjectCanUsing(projectId: number): Observable<ApiResponse<SubInvoice[]>>{
    return this.http.get<ApiResponse<SubInvoice[]>>(this.rootUrl + '/GetAllProjectCanUsing?projectId='+projectId);
  }
  DownloadCVLink(projectId: number) {
    return this.http.get<any>(this.rootUrl + '/DownloadCVLink?projectUserBillId=' + projectId);
  }
  addSubInvoice(item: AddSubInvoiceModel): Observable<ApiResponse<string>>{
    return this.http.post<ApiResponse<string>>(this.rootUrl + '/AddSubInvoice', item);
  }
  getSubInvoiceByProjectId(projectId: number): Observable<ApiResponse<SubInvoice[]>>{
    return this.http.get<ApiResponse<SubInvoice[]>>(this.rootUrl + '/GetSubInoviceByProjectId?projectId='+projectId);
  }
  getAvailableProjectsForSettingInvoice(projectId: number): Observable<ApiResponse<SubInvoice[]>>{
    return this.http.get<ApiResponse<SubInvoice[]>>(this.rootUrl + `/GetAvailableProjectsForSettingInvoice?projectId=${projectId}`)
  }
  updateInvoiceSetting(payload: UpdateInvoiceDto){
    return this.http.post(this.rootUrl + '/UpdateInvoiceSetting', payload)
  }
  getBillInfo(projectId: number): Observable<ApiResponse<ProjectInvoiceSettingDto>>{
    return this.http.get<ApiResponse<ProjectInvoiceSettingDto>>(this.rootUrl + `/GetBillInfo?projectId=${projectId}`)
  }
  checkInvoiceSetting(): Observable<ApiResponse<string>>{
    return this.http.get<ApiResponse<string>>(this.rootUrl + '/CheckInvoiceSetting');
  }

  getAllBillAccount(){
    return this.http.get<any>(this.rootUrl + '/GetAllBillAccount');
  }

  GetAllChargeRoleByProject(projectId: number): Observable<any>{
    return this.http.get<any>(this.rootUrl + `/GetAllChargeRoleByProject?projectId=${projectId}`);
  }
  GetAllLinkedResourcesByProject(projectId: number): Observable<any>{
    return this.http.get<any>(this.rootUrl + `/GetAllLinkedResourcesByProject?projectId=${projectId}`);
  }

  //#endregion
}
