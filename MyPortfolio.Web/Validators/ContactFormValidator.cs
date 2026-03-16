using FluentValidation;
using MyPortfolio.Web.DTOs;

namespace MyPortfolio.Web.Validators;

public class ContactFormValidator : AbstractValidator<ContactFormDto>
{
    public ContactFormValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Vui lòng nhập họ tên.")
            .MaximumLength(100).WithMessage("Tên không được vượt quá 100 ký tự.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Vui lòng nhập email.")
            .EmailAddress().WithMessage("Địa chỉ email không hợp lệ.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Vui lòng nhập nội dung tin nhắn.")
            .MinimumLength(10).WithMessage("Tin nhắn phải có ít nhất 10 ký tự.");
    }
}
