﻿using AutoMapper;

using Convience.Entity.Data;
using Convience.EntityFrameWork.Repositories;
using Convience.Model.Models;
using Convience.Model.Models.SystemManage;
using Convience.Repository;
using Convience.Util.Extension;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Convience.Service.SystemManage
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        private readonly IRoleRepository _roleRepository;

        private readonly IMapper _mapper;

        private readonly IUnitOfWork<SystemIdentityDbContext> _unitOfWork;

        public UserService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IMapper mapper,
            IUnitOfWork<SystemIdentityDbContext> unitOfWork)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<string> AddUserAsync(UserViewModel model)
        {
            await _unitOfWork.StartTransactionAsync();
            try
            {
                var user = _mapper.Map<SystemUser>(model);
                var isSuccess = await _userRepository.AddUserAsync(user);
                if (!isSuccess)
                {
                    await _unitOfWork.RollBackAsync();
                    return "无法创建用户，请检查用户名是否相同！";
                }
                isSuccess = await _userRepository.SetPasswordAsync(user, model.Password);
                if (!isSuccess)
                {
                    await _unitOfWork.RollBackAsync();
                    return "无法创建用户，初始密码创建失败！";
                }
                isSuccess = await _userRepository.AddUserToRoles(user,
                    model.RoleIds.Split(',', StringSplitOptions.RemoveEmptyEntries));
                if (!isSuccess)
                {
                    await _unitOfWork.RollBackAsync();
                    return "无法创建用户，设置角色失败！";
                }
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollBackAsync();
            }

            return string.Empty;
        }

        public async Task<UserResult> GetUserAsync(string Id)
        {
            var user = await _userRepository.GetUserByIdAsync(Id);
            return _mapper.Map<UserResult>(user);
        }

        public IEnumerable<DicModel> GetUserDic(string name)
        {
            var dic = from user in _userRepository.GetUsers()
                      where user.Name.Contains(name)
                      select new DicModel
                      {
                          Key = user.Id.ToString(),
                          Value = user.Name,
                      };
            return dic.Take(10);
        }

        public (IEnumerable<UserResult>, int) GetUsers(UserQuery query)
        {
            Expression<Func<SystemUser, bool>> where = ExpressionExtension.TrueExpression<SystemUser>()
                .AndIfHaveValue(query.UserName, u => u.UserName.Contains(query.UserName))
                .AndIfHaveValue(query.Name, u => u.Name.Contains(query.Name))
                .AndIfHaveValue(query.PhoneNumber, u => u.PhoneNumber.Contains(query.PhoneNumber));

            var userQuery = _userRepository.GetUsers();
            int count;
            if (!string.IsNullOrEmpty(query.RoleId))
            {
                var roleid = int.Parse(query.RoleId);
                userQuery = from u in userQuery
                            join ur in _userRepository.GetUserRoles() on u.Id equals ur.UserId
                            join r in _roleRepository.GetRoles() on ur.RoleId equals roleid
                            select u;
            }

            userQuery = from u in userQuery
                        let q = from ur in _userRepository.GetUserRoles()
                                join r in _roleRepository.GetRoles() on ur.RoleId equals r.Id
                                where ur.UserId == u.Id
                                select r.Id
                        orderby u.Id descending
                        select new SystemUser
                        {
                            Avatar = u.Avatar,
                            Name = u.Name,
                            UserName = u.UserName,
                            PhoneNumber = u.PhoneNumber,
                            Id = u.Id,
                            IsActive = u.IsActive,
                            Sex = u.Sex,
                            CreatedTime = u.CreatedTime,
                            RoleIds = string.Join(',', q.ToArray())
                        };

            var skip = query.Size * (query.Page - 1);
            count = userQuery.Where(where).Count();
            var users = userQuery.Where(where).Skip(skip).Take(query.Size).ToArray();
            return (_mapper.Map<SystemUser[], IEnumerable<UserResult>>(users), count);
        }

        public async Task<string> RemoveUserAsync(string Id)
        {
            await _unitOfWork.StartTransactionAsync();
            try
            {
                var isSuccess = await _userRepository.RemoveUserByIdAsync(Id);
                if (!isSuccess)
                {
                    await _unitOfWork.RollBackAsync();
                    return "删除失败！";
                }
                var count = await _userRepository.GetUserCountInRoleAsync("超级管理员");
                if (count == 0)
                {
                    await _unitOfWork.RollBackAsync();
                    return "可用的超级管理员数量不能为0！";
                }
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollBackAsync();
            }
            return string.Empty;
        }

        public async Task<string> RemoveUserByNameAsync(string Name)
        {
            await _unitOfWork.StartTransactionAsync();
            try
            {
                var isSuccess = await _userRepository.RemoveUserByNameAsync(Name);
                if (!isSuccess)
                {
                    await _unitOfWork.RollBackAsync();
                    return "删除失败！";
                }
                var count = await _userRepository.GetUserCountInRoleAsync("超级管理员");
                if (count == 0)
                {
                    await _unitOfWork.RollBackAsync();
                    return "可用的超级管理员数量不能为0！";
                }
                await _unitOfWork.CommitAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollBackAsync();
            }

            return string.Empty;
        }

        public async Task<bool> ResetUserPassword(string userName, string password)
        {
            var user = await _userRepository.GetUserByNameAsync(userName);
            return user != null ?
                await _userRepository.ResetPasswordAsync(user, password) : false;
        }

        public async Task<bool> SetUserPassword(string userName, string password)
        {
            var user = await _userRepository.GetUserByNameAsync(userName);
            return user != null ?
                await _userRepository.SetPasswordAsync(user, password) : false;
        }

        public async Task<string> UpdateUserAsync(UserViewModel model)
        {
            await _unitOfWork.StartTransactionAsync();
            try
            {
                var user = await _userRepository.GetUserByIdAsync(model.Id.ToString());
                if (user != null)
                {
                    _mapper.Map(model, user);
                    var isSuccess = await _userRepository.UpdateUserAsync(user);
                    if (!isSuccess)
                    {
                        await _unitOfWork.RollBackAsync();
                        return "无法更新用户，请检查用户名是否相同！";
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(model.Password))
                        {
                            isSuccess = await _userRepository.ResetPasswordAsync(user, model.Password);
                            if (!isSuccess)
                            {
                                await _unitOfWork.RollBackAsync();
                                return "更新密码失败！";
                            }
                        }
                    }

                    isSuccess = await _userRepository.AddUserToRoles(user,
                        model.RoleIds.Split(',', StringSplitOptions.RemoveEmptyEntries));
                    if (!isSuccess)
                    {
                        await _unitOfWork.RollBackAsync();
                        return "无法更新用户，更新角色失败！";
                    }

                    var count = await _userRepository.GetUserCountInRoleAsync("超级管理员");
                    if (count == 0)
                    {
                        await _unitOfWork.RollBackAsync();
                        return "可用的超级管理员数量不能为0！";
                    }
                    await _unitOfWork.CommitAsync();
                }
            }
            catch (Exception)
            {
                await _unitOfWork.RollBackAsync();
                return "用户更新失败，请练习管理员！";
            }

            return string.Empty;
        }

    }
}
