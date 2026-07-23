using KurisuCore.Models.Bases;

namespace KurisuCore.Models.Data;

public sealed record KUserData(
    long Id,
    KSettings Settings,
    DateTime LastQuizRegen,
    int QuizTokens,
    int Pity,
    int Coins,
    int RDust,
    int SrDust,
    int SsrDust,
    int UrDust) 
    : IKData<long>;