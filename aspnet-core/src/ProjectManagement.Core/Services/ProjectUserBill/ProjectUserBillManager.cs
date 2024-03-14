﻿using Abp.Application.Services;
using NccCore.IoC;
using ProjectManagement.Authorization.Users;
using ProjectManagement.Authorization;
using ProjectManagement.Entities;
using ProjectManagement.Services.ResourceManager.Dto;
using ProjectManagement.Services.ResourceService.Dto;
using System.Linq;
using static ProjectManagement.Constants.Enum.ProjectEnum;
using System.Threading.Tasks;
using Abp.Collections.Extensions;
using Microsoft.EntityFrameworkCore;
using NccCore.Paging;
using NccCore.Extension;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Collections.Generic;
using ProjectManagement.Services.ProjectUserBill.Dto;
using Abp.UI;
using Abp.Domain.Uow;
using Abp;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.AspNetCore.SignalR;
using System;
using Abp.Linq.Expressions;
using ProjectManagement.Services.ProjectUserBill;

namespace ProjectManagement.Services.ProjectUserBills
{
    public class ProjectUserBillManager : ApplicationService
    {
        private readonly IWorkScope _workScope;

        public ProjectUserBillManager(IWorkScope workScope)
        {
            _workScope = workScope;
        }

        public IQueryable<ProjectManagement.Entities.ProjectUserBill> GetSubProjectBills(long parentProjectId)
        {
            var query = from p in _workScope.GetAll<Project>()
                        where p.ParentInvoiceId == parentProjectId
                        join pub in _workScope.GetAll<ProjectManagement.Entities.ProjectUserBill>()
                         on p.Id equals pub.ProjectId
                        select pub;
            return query.OrderBy(p => p.Project.Name).ThenBy(p => p.User.EmailAddress);
        }

        public async Task<List<GetAllResourceDto>> QueryAllResource(bool isVendor, List<long> existResourceIds = null)
        {
            // get current user and view user level permission
            // if user level = intern => all show no matter the permission
            var listLoginUserPM = _workScope.GetAll<ProjectUser>()
                .Where(pu => pu.Project.Status != ProjectStatus.Closed
                    && (pu.Status == ProjectUserStatus.Present && pu.AllocatePercentage > 0
                    || pu.Status == ProjectUserStatus.Future))
                .Where(pu => pu.UserId == AbpSession.UserId.GetValueOrDefault() && pu.ProjectRole == 0 || pu.Project.PMId == AbpSession.UserId.GetValueOrDefault()
                    ).Select(pu => pu.Id);

            var activeReportId = await GetActiveReportId();

            var quser = _workScope.GetAll<User>()
                       .Where(x => x.IsActive)
                       .Where(x => x.UserType != UserType.FakeUser)
                       .Where(u => isVendor ? u.UserType == UserType.Vendor : u.UserType != UserType.Vendor)
                       .Select(x => new GetAllResourceDto
                       {
                           UserId = x.Id,
                           UserType = x.UserType,
                           FullName = x.Name + " " + x.Surname,
                           NormalFullName = x.Surname + " " + x.Name,
                           EmailAddress = x.EmailAddress,
                           Branch = x.BranchOld,
                           BranchColor = x.Branch.Color,
                           BranchDisplayName = x.Branch.DisplayName,
                           BranchId = x.BranchId,
                           PositionId = x.PositionId,
                           PositionColor = x.Position.Color,
                           PositionName = x.Position.ShortName,
                           UserLevel = x.UserLevel >= UserLevel.Intern_0
                                && x.UserLevel <= UserLevel.Intern_3 ? x.UserLevel :
                                _workScope.GetAll<ProjectUser>().Any(pu => pu.UserId == x.Id
                                && listLoginUserPM.Contains(pu.Id)) ? x.UserLevel : default(UserLevel?),
                           AvatarPath = x.AvatarPath,
                           StarRate = x.StarRate,
                           UserSkills = x.UserSkills.Select(s => new UserSkillDto
                           {
                               UserId = s.UserId,
                               SkillId = s.SkillId,
                               SkillName = s.Skill.Name,
                               SkillRank = s.SkillRank
                           }).ToList(),

                           PlanProjects = x.ProjectUsers
                           .Where(pu => pu.Status == ProjectUserStatus.Future)
                           .Where(pu => pu.Project.Status != ProjectStatus.Closed)
                           .Where(pu => pu.PMReportId == activeReportId)
                           .Select(pu => new ProjectOfUserDto
                           {
                               Id = pu.Id,
                               ProjectId = pu.ProjectId,
                               ProjectName = pu.Project.Name,
                               ProjectRole = pu.ProjectRole,
                               PmName = pu.Project.PM.Name,
                               StartTime = pu.StartTime,
                               IsPool = pu.IsPool,
                               AllocatePercentage = pu.AllocatePercentage,
                               ProjectType = pu.Project.ProjectType,
                               ProjectCode = pu.Project.Code
                           })
                           .ToList(),

                           WorkingProjects = x.ProjectUsers
                            .Where(s => s.Status == ProjectUserStatus.Present
                            && s.AllocatePercentage > 0
                            && s.Project.Status != ProjectStatus.Closed)
                            .Select(pu => new ProjectOfUserDto
                            {
                                Id = pu.Id,
                                ProjectId = pu.ProjectId,
                                ProjectName = pu.Project.Name,
                                ProjectRole = pu.ProjectRole,
                                ProjectStatus = pu.Project.Status,
                                PmName = pu.Project.PM.Name,
                                StartTime = pu.StartTime,
                                IsPool = pu.IsPool,
                                ProjectType = pu.Project.ProjectType,
                                ProjectCode = pu.Project.Code
                            })
                           .ToList(),
                       })
                       .WhereIf(existResourceIds != null && existResourceIds.Any(), s => !existResourceIds.Contains(s.UserId))
                       .ToList();

            return quser;
        }

        public async Task<long> GetActiveReportId()
        {
            return await _workScope.GetAll<PMReport>()
                .Where(s => s.IsActive == true)
                .OrderByDescending(s => s.Id)
                .Select(s => s.Id).FirstOrDefaultAsync();
        }

        public IQueryable<T> ApplyOrders<T>(IQueryable<T> query, IDictionary<string, SortDirection> sortParams)
        {
            if (query == null || sortParams == null) return query;
            var isOrder = true;
            var queryOrder = (IOrderedQueryable<T>)query;
            foreach (var param in sortParams)
            {
                queryOrder = OrderByProperty(queryOrder, param.Key, param.Value, isOrder);
                isOrder = false;
            }
            return queryOrder;
        }

        private IOrderedQueryable<T> OrderByProperty<T>(IQueryable<T> query, string propertyName, SortDirection sortDirection, bool anotherLevel)
        {
            ParameterExpression parameterExpression = Expression.Parameter(typeof(T), string.Empty);
            MemberExpression memberExpression = Expression.PropertyOrField(parameterExpression, propertyName);
            LambdaExpression lambdaExpression = Expression.Lambda(memberExpression, parameterExpression);
            MethodCallExpression methodCallExpression = Expression.Call(
                typeof(Queryable),
                (anotherLevel ? "OrderBy" : "ThenBy") + (sortDirection == SortDirection.DESC ? "Descending" : string.Empty),
                new[] { typeof(T), memberExpression.Type },
                query.Expression,
                Expression.Quote(lambdaExpression)
            );
            var result = (IOrderedQueryable<T>)query.Provider.CreateQuery(methodCallExpression);
            return result;
        }

        public async Task<List<GetProjectUserBillDto>> GetAllByProject(GetAllProjectUserBillDto input)
        {
            var isViewRate = await IsGrantedAsync(PermissionNames.Projects_OutsourcingProjects_ProjectDetail_TabBillInfo_Rate_View);

            var query = await _workScope.GetAll<ProjectManagement.Entities.ProjectUserBill>()
                .Where(x => x.ProjectId == input.ProjectId)
                .FilterByChargeStatus(input.ChargeStatus)
                .FilterByChargeName(input.ChargeNameFilter)
                .FilterByChargeRole(input.ChargeRoleFilter)
                .FilterByChargeType(input.ChargeType)
                .Select(x => new GetProjectUserBillDto
                {
                Id = x.Id,
                UserId = x.UserId,
                UserName = x.User.Name,
                ProjectId = x.ProjectId,
                ProjectName = x.Project.Name,
                AccountName = x.AccountName,
                BillRole = x.BillRole,
                BillRate = isViewRate ? x.BillRate : 0,
                StartTime = x.StartTime.Date,
                EndTime = x.EndTime.Value.Date,
                Note = x.Note,
                shadowNote = x.shadowNote,
                isActive = x.isActive,
                AvatarPath = x.User.AvatarPath,
                FullName = x.User.FullName,
                Branch = x.User.BranchOld,
                BranchColor = x.User.Branch.Color,
                BranchDisplayName = x.User.Branch.DisplayName,
                PositionId = x.User.PositionId,
                PositionName = x.User.Position.ShortName,
                PositionColor = x.User.Position.Color,
                EmailAddress = x.User.EmailAddress,
                UserType = x.User.UserType,
                UserLevel = x.User.UserLevel,
                ChargeType = x.ChargeType ?? x.Project.ChargeType,
                CreationTime = x.CreationTime,

                LinkedResources = x.LinkedResources
                        .Select(lr => new GetUserInfo
                        {
                            Id = lr.UserId,
                            EmailAddress = lr.User.EmailAddress,
                            UserName = lr.User.UserName,
                            AvatarPath = lr.User.AvatarPath ?? "",
                            UserType = lr.User.UserType,
                            PositionId = lr.User.PositionId,
                            PositionColor = lr.User.Position.Color,
                            PositionName = lr.User.Position.ShortName,
                            UserLevel = lr.User.UserLevel,
                            BranchColor = lr.User.Branch.Color,
                            BranchDisplayName = lr.User.Branch.DisplayName,
                            IsActive = lr.User.IsActive,
                            FullName = lr.User.FullName,
                        }).ToList()
                })
                .ApplySearch(input)
                .OrderByDescending(x => x.CreationTime)
                .ToListAsync();
            return query;
        }

        public async Task<List<UserDto>> GetAllUserActive(bool onlyStaff, long projectId, long? currentUserId)
        {
            var listPUBIds = await _workScope.GetAll<ProjectManagement.Entities.ProjectUserBill>()
                .Where(x => x.ProjectId == projectId)
                .Select(x => x.UserId)
                .ToListAsync();

            if (currentUserId.HasValue)
                listPUBIds = listPUBIds.Where(x => x != currentUserId).ToList();

            var query = _workScope.GetAll<User>()
                .Where(u => u.IsActive)
                .Where(x => x.UserType != UserType.Vendor)
                .Where(x => x.UserType != UserType.FakeUser)
                .Where(x => onlyStaff ? x.UserType != UserType.Internship : true)
                .Where(x => !listPUBIds.Contains(x.Id))
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName,
                    Name = u.Name,
                    Surname = u.Surname,
                    EmailAddress = u.EmailAddress,
                    FullName = u.FullName,
                    AvatarPath = u.AvatarPath,
                    UserType = u.UserType,
                    UserLevel = u.UserLevel,
                    Branch = u.BranchOld,
                    PositionId = u.PositionId,
                    UserSkills = u.UserSkills.Select(x => new UserSkillDto
                    {
                        SkillId = x.SkillId,
                        SkillName = x.Skill.Name
                    }).ToList()
                })
                .Distinct()
                .ToListAsync();
            return await query;
        }

        public async Task<List<ProjectUserBillAccount>> AddProjectUserBillAccounts(ProjectUserBillAccountsDto input)
        {
            var listPUBA = new List<ProjectUserBillAccount>();
            foreach (var userId in input.UserIds)
            {
                await ValidateProjectUserBillAccount(userId, input.ProjectId, input.BillAccountId);

                var pUBA = new ProjectUserBillAccount();

                using (UnitOfWorkManager.Current.DisableFilter(AbpDataFilters.SoftDelete))
                {
                    pUBA = await _workScope.GetAll<ProjectUserBillAccount>()
                     .Where(s => s.UserBillAccountId == input.BillAccountId && s.UserId == userId && s.ProjectId == input.ProjectId)
                     .FirstOrDefaultAsync();
                }

                if (pUBA != null && !pUBA.IsDeleted)
                {
                    throw new UserFriendlyException($"UserBillAccount {pUBA.Id} already exists!");
                }
                else if (pUBA != null && pUBA.IsDeleted)
                {
                    pUBA.IsDeleted = false;
                    await _workScope.UpdateAsync(pUBA);
                    listPUBA.Add(pUBA);
                }
                else if (pUBA == null)
                {

                    var newBA = new ProjectUserBillAccount
                    {
                        UserId = userId,
                        ProjectId = input.ProjectId,
                        UserBillAccountId = input.BillAccountId,
                    };

                    await _workScope.InsertAsync(newBA);
                    listPUBA.Add(newBA);
                }
            }

            return listPUBA;
        }

        public async Task LinkOneProjectUserBillAccount(ProjectUserBillAccountDto input)
        {
            await ValidateProjectUserBillAccount(input.UserId, input.ProjectId, input.BillAccountId);

            var pUBA = new ProjectUserBillAccount();

            using (UnitOfWorkManager.Current.DisableFilter(AbpDataFilters.SoftDelete))
            {
                pUBA = await _workScope.GetAll<ProjectUserBillAccount>()
                    .Where(s => s.UserBillAccountId == input.BillAccountId && s.ProjectId == input.ProjectId && s.UserId == input.UserId)
                    .FirstOrDefaultAsync();
            }
            if (pUBA != null && !pUBA.IsDeleted)
            {
                throw new UserFriendlyException($"UserBillAccount {pUBA.Id} already exists in project {pUBA.Project.Name}!");
            }
            else if (pUBA != null && pUBA.IsDeleted)
            {
                pUBA.IsDeleted = false;
                await _workScope.UpdateAsync(pUBA);
            }
            else if (pUBA == null)
            {
                var newBA = new ProjectUserBillAccount
                {
                    UserId = input.UserId,
                    ProjectId = input.ProjectId,
                    UserBillAccountId = input.BillAccountId,
                };

                await _workScope.InsertAsync(newBA);
            }
        }

        public async Task RemoveProjectUserBillAccount(ProjectUserBillAccountsDto input)
        {
            foreach (var userId in input.UserIds)
            {
                await ValidateProjectUserBillAccount(userId, input.ProjectId, input.BillAccountId);

                var pUBA = await _workScope.GetAll<ProjectUserBillAccount>()
                   .Where(s => s.UserBillAccountId == input.BillAccountId && s.UserId == userId && s.ProjectId == input.ProjectId)
                   .FirstOrDefaultAsync();

                if (pUBA != null)
                    await _workScope.DeleteAsync(pUBA);
            }
        }

        private async Task ValidateProjectUserBillAccount(long userId, long projectId, long billAccountId)
        {
            var existedUser = await _workScope.GetAll<User>()
              .Where(s => s.Id == userId)
              .FirstOrDefaultAsync();
            if (existedUser == null)
                throw new UserFriendlyException($"User with Id {userId} does not exist!");

            var existedPUB = await _workScope.GetAll<ProjectManagement.Entities.ProjectUserBill>()
               .Where(x => x.UserId == billAccountId && x.ProjectId == projectId)
               .FirstOrDefaultAsync();
            if (existedPUB == null)
                throw new UserFriendlyException($"BillAccount(User) {billAccountId} is not working in Project {projectId}!");
        }

        public async Task<List<BillAccountDto>> GetAllBillAccount()
        {
            return await _workScope.GetAll<Entities.ProjectUserBill>()
                .Select(p => new BillAccountDto()
                {
                    EmailAddress = p.User.EmailAddress,
                    UserId = p.User.Id
                })
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<LinkedResource>> AddLinkedResources(LinkedResourcesDto input)
        {
            var listLinkedResources = new List<LinkedResource>();

            ValidateProjectUserBill(input.ProjectUserBillId);

            foreach (var userId in input.UserIds)
            {
                ValidateUser(userId);

                var existingLinkedResource = await _workScope.GetAll<LinkedResource>()
                    .Where(lr => lr.UserId == userId && lr.ProjectUserBillId == input.ProjectUserBillId)
                    .FirstOrDefaultAsync();

                if (existingLinkedResource != null)
                {
                    existingLinkedResource.IsDeleted = false;
                    await _workScope.UpdateAsync(existingLinkedResource);
                    listLinkedResources.Add(existingLinkedResource);
                }
                else
                {
                    var newLinkedResource = new LinkedResource
                    {
                        UserId = userId,
                        ProjectUserBillId = input.ProjectUserBillId,
                    };

                    await _workScope.InsertAsync(newLinkedResource);
                    listLinkedResources.Add(newLinkedResource);
                }
            }

            return listLinkedResources;
        }

        public async Task LinkOneLinkedResource(LinkedResourceDto input)
        {
            ValidateProjectUserBill(input.ProjectUserBillId);

            ValidateUser(input.UserId);


            var newLinkedResource = new LinkedResource
            {
                UserId = input.UserId,
                ProjectUserBillId = input.ProjectUserBillId,
            };

            await _workScope.InsertAsync(newLinkedResource);
        }

        public async Task RemoveLinkedResource(LinkedResourcesDto input)
        {
            ValidateProjectUserBill(input.ProjectUserBillId);

            foreach (var userId in input.UserIds)
            {
                ValidateUser(userId);

                var existingLinkedResource = await _workScope.GetAll<LinkedResource>()
                    .Where(lr => lr.UserId == userId && lr.ProjectUserBillId == input.ProjectUserBillId)
                    .FirstOrDefaultAsync();

                if (existingLinkedResource != null)
                    await _workScope.DeleteAsync(existingLinkedResource);
            }
        }

        private void ValidateUser(long userId)
        {
            var existedUser = _workScope.GetAll<User>()
                .Where(s => s.Id == userId)
                .Any();

            if (!existedUser)
                throw new UserFriendlyException($"User with Id {userId} does not exist!");
        }

        private void ValidateProjectUserBill(long projectUserBillId)
        {
            var isExist = _workScope.GetAll<Entities.ProjectUserBill>()
                .Where(s => s.Id == projectUserBillId)
                .Any();

            if (!isExist)
                throw new UserFriendlyException($"ProjectUserBill with Id {projectUserBillId} does not exist!");
        }

    }
}