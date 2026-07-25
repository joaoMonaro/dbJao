using System;
using System.Net.Http;
using System.Threading.Tasks;

public static class AuthErrorMessages
{
    public static string From(Exception exception) => exception switch
    {
        ApiRequestException { StatusCode: 401 } =>
            "Email ou senha inválidos.",
        ApiRequestException { StatusCode: 429 } =>
            "Muitas tentativas. Aguarde um pouco.",
        ApiRequestException { StatusCode: 503 } =>
            "O serviço está temporariamente indisponível.",
        ApiRequestException { ErrorCode: "RESET_TOKEN_EXPIRED" } =>
            "Este link de recuperação expirou. Solicite um novo.",
        ApiRequestException { ErrorCode: "RESET_TOKEN_INVALID" } =>
            "Não foi possível redefinir a senha.",
        ApiRequestException request when
            request.Message.Contains("Username", StringComparison.OrdinalIgnoreCase) =>
            "Este nome de usuário já está em uso.",
        ApiRequestException request when
            request.Message.Contains("mail", StringComparison.OrdinalIgnoreCase) =>
            "Este email já está em uso.",
        ApiRequestException { StatusCode: 400 } =>
            "Verifique os dados informados.",
        HttpRequestException =>
            "Não foi possível conectar ao servidor.",
        TaskCanceledException =>
            "A solicitação demorou demais. Tente novamente.",
        _ => "Não foi possível concluir a operação.",
    };
}
