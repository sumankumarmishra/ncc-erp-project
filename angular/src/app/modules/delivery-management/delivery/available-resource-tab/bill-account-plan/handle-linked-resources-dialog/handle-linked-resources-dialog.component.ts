import { catchError } from "rxjs/operators";
import { AppComponentBase } from "@shared/app-component-base";
import { Component, OnInit, Injector, Inject, ViewChild } from "@angular/core";
import { ProjectUserBillService } from "@app/service/api/project-user-bill.service";
import { MAT_DIALOG_DATA, MatDialogRef } from "@angular/material/dialog";
import { forkJoin } from "rxjs";

@Component({
  selector: 'app-handle-linked-resources-dialog',
  templateUrl: './handle-linked-resources-dialog.component.html',
  styleUrls: ['./handle-linked-resources-dialog.component.css']
})
export class HandleLinkedResourcesDialogComponent
    extends AppComponentBase
    implements OnInit
{
    listAllResource = []
    userId: number;
    projectUserBillId: number;
    listResourceSelect = []
    listResourceSelected = []
    listResourceSelectCurrent = []
    resourceSelected = [];
    resourceUnSelected = [];
    searchUser = '';
    constructor(
        injector: Injector,
        public dialogRef: MatDialogRef<HandleLinkedResourcesDialogComponent>,
        @Inject(MAT_DIALOG_DATA) public data: any,
        private projectUserBillService: ProjectUserBillService
    ) {
        super(injector);
    }
    @ViewChild("select") select;
    ngOnInit() {
        this.listResourceSelect = this.data.listResource;
        this.listResourceSelected = [...this.data.listResource];
        this.listResourceSelectCurrent = [...this.listResourceSelect];
        this.listAllResource = this.data.listAllResource.filter(item => !this.listResourceSelect.includes(item.userId));
    }

    openedChange(event) {
        if (!event) {
            this.searchUser = ''
        }
    }
    onSelectChange(id) {
        if (this.listResourceSelectCurrent.includes(id)) {
            this.listResourceSelectCurrent = this.listResourceSelectCurrent.filter(res => res != id)
            this.listResourceSelect = [...this.listResourceSelectCurrent]
        }
        else {
            this.listResourceSelectCurrent.push(id)
            this.listResourceSelect = [...this.listResourceSelectCurrent]
        }
        this.orderListResource()
    }

    orderListResource() {
        this.resourceSelected = this.listAllResource.filter(item => this.listResourceSelect.includes(item.userId))
        this.resourceUnSelected = this.listAllResource.filter(item => !this.listResourceSelect.includes(item.userId))
        this.listAllResource = [...this.resourceSelected, ...this.resourceUnSelected]
    }

    save() {
        const reqAdd = {
            projectUserBillId: this.data.projectUserBillId,
            userIds: this.listResourceSelect.filter(item => !this.listResourceSelected.includes(item))
        };

        let linkedResourceUpdate;

        if (reqAdd.userIds.length > 0) {
            linkedResourceUpdate = this.projectUserBillService.LinkUserToBillAccount(reqAdd);
        } else {
            abp.notify.success("Linked resources updated successfully");
            this.dialogRef.close({
                userIdNew: this.data.userIdNew,
                isSave: true
            });
        }

        linkedResourceUpdate.pipe(
            catchError(this.projectUserBillService.handleError)
        ).subscribe(result => {
            abp.notify.success("Linked resources updated successfully");
            this.dialogRef.close({
                userIdNew: this.data.userIdNew,
                isSave: true
            });
        });
    }


    clear() {
        this.listResourceSelect = [];
        this.listResourceSelectCurrent = [];
        this.resourceSelected = this.listAllResource.filter(item => this.listResourceSelect.includes(item.userId))
    }
    selectAll() {
        this.listResourceSelect = this.listAllResource.map(item => item.userId)
        this.listResourceSelectCurrent = [... this.listResourceSelect]
        this.resourceSelected = this.listAllResource.filter(item => this.listResourceSelect.includes(item.userId))
    }
}
