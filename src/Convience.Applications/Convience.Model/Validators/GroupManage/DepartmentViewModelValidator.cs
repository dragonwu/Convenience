﻿using Convience.Model.Models.GroupManage;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Text;

namespace Convience.Model.Validators.GroupManage
{
    public class DepartmentViewModelValidator : AbstractValidator<DepartmentViewModel>
    {
        public DepartmentViewModelValidator()
        {
            RuleFor(viewmodel => viewmodel.Name).NotNull().NotEmpty()
                .WithMessage("部门名称不能为空!");

            RuleFor(viewmodel => viewmodel.Name).MaximumLength(15)
                .WithMessage("部门名称长度不能超过15!");

            RuleFor(viewmodel => viewmodel.Email).MaximumLength(50)
                .WithMessage("邮件长度不能超过50!");

            RuleFor(viewmodel => viewmodel.Telephone).MaximumLength(20)
                .WithMessage("电话长度不能超过20!");
        }
    }
}