using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace CORE_BE.Models
{
    public class LoginModel
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
    }

    public class UserToken
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public bool MustChangePass { get; set; }
    }

    public class InfoLogin
    {
        public string Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
        public DateTime? Expires { get; set; }
        public bool MustChangePass { get; set; }
    }

    public class RegisterModel
    {
        private string _UserName { get; set; }

        [Required]
        [StringLength(256)]
        public string UserName
        {
            get { return _UserName; }
            set { _UserName = value.Trim(); }
        }
        private string _Email { get; set; }

        [StringLength(256)]
        public string Email
        {
            get { return _Email; }
            set { _Email = value.Trim(); }
        }
        private string _FullName { get; set; }
        public string FullName
        {
            get { return _FullName; }
            set { _FullName = value.Trim(); }
        }
        public List<string> RoleNames { get; set; }
        public bool IsActive { get; set; }
        private string _GhiChu { get; set; }
        public string GhiChu
        {
            get { return _GhiChu; }
            set { _GhiChu = value?.Trim(); }
        }
        public List<Guid>? ListDonVi { get; set; }
    }

    public class UserInfoModel
    {
        public string Id { get; set; }
        private string _UserName { get; set; }

        [Required]
        [StringLength(256)]
        public string UserName
        {
            get { return _UserName; }
            set { _UserName = value.Trim(); }
        }
        private string _Email { get; set; }

        [StringLength(256)]
        public string Email
        {
            get { return _Email; }
            set { _Email = value.Trim(); }
        }
        private string _FullName { get; set; }
        public string FullName
        {
            get { return _FullName; }
            set { _FullName = value.Trim(); }
        }
        public List<string> RoleNames { get; set; }
        public bool IsActive { get; set; }
        private string _GhiChu { get; set; }
        public string GhiChu
        {
            get { return _GhiChu; }
            set { _GhiChu = value?.Trim(); }
        }
        public string UserCode { get; set; }
        public List<Guid> ListDonVi { get; set; }
    }

    public class ChangePasswordModel
    {
        [Required]
        [StringLength(
            100,
            ErrorMessage = "Mật khẩu {0} ngắn nhất phải {2} ký tự.",
            MinimumLength = 6
        )]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string Password { get; set; }

        [Required]
        [StringLength(
            100,
            ErrorMessage = "Mật khẩu {0} ngắn nhất phải {2} ký tự.",
            MinimumLength = 6
        )]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu xác nhận")]
        [Compare("NewPassword", ErrorMessage = "Xác nhận mật khẩu mới không đúng.")]
        public string ConfirmNewPassword { get; set; }
    }

    public class MenuInfo
    {
        public Guid Id { get; set; }
        public string STT { get; set; }
        public string TenMenu { get; set; }
        public string Url { get; set; }
        public string Icon { get; set; }
        public int ThuTu { get; set; }
        public Guid? Parent_Id { get; set; }
        public List<MenuInfo> children { set; get; }
        public bool IsUsed { get; set; }
        public bool IsRemove { get; set; }
    }

    public class Permission
    {
        public bool View { get; set; }
        public bool Add { get; set; }
        public bool Edit { get; set; }
        public bool Del { get; set; }
        public bool Print { get; set; }
        public bool Cof { get; set; }
    }

    public class MenuView
    {
        public Guid Id { get; set; }
        public string STT { get; set; }
        public string TenMenu { get; set; }
        public string Url { get; set; }
        public Guid? Parent_Id { get; set; }
        public int ThuTu { get; set; }
        public string Icon { get; set; }
        public List<MenuView> children { set; get; }
        public Permission permission { set; get; }
    }
}
