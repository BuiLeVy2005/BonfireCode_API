import sys

with open('Controllers/AuthController.cs', 'r', encoding='utf-8') as f:
    code = f.read()

# 1. Add using System.Net; and using System.Net.Mail;
if 'using System.Net.Mail;' not in code:
    code = 'using System.Net;\nusing System.Net.Mail;\n' + code

# 2. Add SendEmailAsync method inside the class
send_email_code = """
        private async Task SendEmailAsync(string toEmail, string otp)
        {
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];
            
            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
            {
                throw new Exception("Email settings are not configured in appsettings.");
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(senderEmail, "BonfireCode Support"),
                Subject = "Mã xác nhận khôi phục mật khẩu (OTP)",
                Body = $"<div style='font-family: Inter, Arial, sans-serif; padding: 20px; max-width: 600px; margin: 0 auto; border: 1px solid #30363d; border-radius: 10px; background-color: #0d1117; color: #c9d1d9;'>" +
                       $"<h2 style='color: #a855f7; text-align: center; font-size: 24px; font-weight: bold;'>BonfireCode</h2>" +
                       $"<p style='font-size: 16px;'>Chào bạn,</p>" +
                       $"<p style='font-size: 14px;'>Bạn vừa yêu cầu khôi phục mật khẩu. Dưới đây là mã OTP 6 chữ số của bạn:</p>" +
                       $"<div style='text-align: center; margin: 30px 0;'>" +
                       $"<span style='font-size: 28px; font-weight: 700; padding: 15px 30px; background-color: #161b22; border: 1px solid #30363d; border-radius: 8px; letter-spacing: 8px; color: #fff;'>{otp}</span>" +
                       $"</div>" +
                       $"<p style='font-size: 14px;'>Mã này sẽ <strong>hết hạn trong vòng 15 phút</strong>. Vui lòng không chia sẻ mã này cho bất kỳ ai để đảm bảo an toàn cho tài khoản của bạn.</p>" +
                       $"<br><hr style='border: none; border-top: 1px solid #30363d;' />" +
                       $"<p style='font-size: 12px; color: #8b949e;'>Trân trọng,<br>Đội ngũ BonfireCode - Đồ án Niên luận Võ Chí Hải</p>" +
                       $"</div>",
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            using var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true,
            };

            await smtpClient.SendMailAsync(mailMessage);
        }
"""

# Find constructor end
constructor_idx = code.find('public AuthController(ApplicationDbContext context, IConfiguration configuration)')
constructor_end_idx = code.find('}', constructor_idx) + 1

code = code[:constructor_end_idx] + '\n' + send_email_code + code[constructor_end_idx:]

# 3. Modify ForgotPassword
old_forgot = 'return Ok(new { Message = "Giả lập gửi email thành công.", OTP = otp });'
new_forgot = """
            try
            {
                await SendEmailAsync(user.Email, otp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Lỗi khi kết nối với máy chủ gửi Email.", Error = ex.Message });
            }

            return Ok(new { Message = "Mã OTP đã được gửi đến email của bạn." });"""

code = code.replace(old_forgot, new_forgot)

with open('Controllers/AuthController.cs', 'w', encoding='utf-8') as f:
    f.write(code)

print('Success')