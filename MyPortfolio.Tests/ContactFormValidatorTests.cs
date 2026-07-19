using Xunit;
using MyPortfolio.Web.Validators;
using MyPortfolio.Web.DTOs;
using FluentValidation.TestHelper;

namespace MyPortfolio.Tests
{
    public class ContactFormValidatorTests
    {
        private readonly ContactFormValidator _validator;

        public ContactFormValidatorTests()
        {
            _validator = new ContactFormValidator();
        }

        [Fact]
        public void Should_Have_Error_When_Name_Is_Empty()
        {
            var model = new ContactFormDto { Name = "", Email = "test@example.com", Message = "This is a valid message." };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Vui lòng nhập họ tên.");
        }

        [Fact]
        public void Should_Have_Error_When_Name_Exceeds_MaxLength()
        {
            var model = new ContactFormDto 
            { 
                Name = new string('A', 101), 
                Email = "test@example.com", 
                Message = "This is a valid message." 
            };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Name)
                  .WithErrorMessage("Tên không được vượt quá 100 ký tự.");
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Empty()
        {
            var model = new ContactFormDto { Name = "John Doe", Email = "", Message = "This is a valid message." };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Vui lòng nhập email.");
        }

        [Fact]
        public void Should_Have_Error_When_Email_Is_Invalid()
        {
            var model = new ContactFormDto { Name = "John Doe", Email = "invalid-email", Message = "This is a valid message." };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Email)
                  .WithErrorMessage("Địa chỉ email không hợp lệ.");
        }

        [Fact]
        public void Should_Have_Error_When_Message_Is_Empty()
        {
            var model = new ContactFormDto { Name = "John Doe", Email = "test@example.com", Message = "" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Message)
                  .WithErrorMessage("Vui lòng nhập nội dung tin nhắn.");
        }

        [Fact]
        public void Should_Have_Error_When_Message_Is_Too_Short()
        {
            var model = new ContactFormDto { Name = "John Doe", Email = "test@example.com", Message = "Short" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Message)
                  .WithErrorMessage("Tin nhắn phải có ít nhất 10 ký tự.");
        }

        [Fact]
        public void Should_Not_Have_Errors_When_Model_Is_Valid()
        {
            var model = new ContactFormDto { Name = "John Doe", Email = "test@example.com", Message = "This is a valid message." };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
