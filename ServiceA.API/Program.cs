using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using ServiceA.API.Services;
using System.Diagnostics;
using System.Runtime.CompilerServices;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
//tüm akışları bunun üstünden gercekleştiricez. Network exception hatası için retry yapsın b ayakta degilse, 500 ile başlayanlar ve 408 timeout hatalarında
var retry = HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), onRetry: (arg1, arg2) =>
{
    Debug.WriteLine($"Retry is made again:{arg2.TotalMilliseconds}");
});//2 4 8 saniye olarak gidecek yani toplamda 3 kez gidecek. 5 den fazla yapmamak lazım memorye yuk bindirmemek için.retry yeterli degil circuit breaker da gerekli. bu pollyde 2 şekilde gercekleşiyor.
//retryAttempt:her bir retry arasındaki süre
var retry2 = HttpPolicyExtensions.HandleTransientHttpError().OrResult(MSG => MSG.StatusCode == System.Net.HttpStatusCode.NotFound).WaitAndRetryAsync(5, retryAttempt =>
{
Debug.WriteLine($"RetryCount: {retryAttempt }");
return TimeSpan.FromSeconds(10);
}, onRetryAsync: async (DelegateResult<HttpResponseMessage> arg1, TimeSpan arg2) =>
{
    Debug.WriteLine($"Request is made again:{arg2.TotalMilliseconds}");

    await Task.CompletedTask;
});//CUSTOM HATA MESAJLARI İÇİN  //rertry olmadan önce de kod calıştırmak isteyebilirz
//WaitAndRetryAsync için 19 adet overload var

//circuit breaker (basic) bir süre olmaksızın arka arkaya 3 hata mesajı olunca 5 saniye boyunca devreyi acık bırak.polly kendiis bi excepiton fırlatıyor bu şekilde anlayabilirz.16.saniyede tekrar istek atacak ya devre acık duruma devam edecek ya da kapalıya gececek
var circuitBreakerPolicy = HttpPolicyExtensions.HandleTransientHttpError().CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 3, durationOfBreak: TimeSpan.FromSeconds(5), onBreak:(arg1, arg2) =>
{
    Debug.WriteLine("Circuit breaker status => On Break");
}, onReset:() =>
{
    Debug.WriteLine("Circuit breaker status => On Reset");
},
onHalfOpen:() =>                                    
{
    Debug.WriteLine("Circuit breaker status => On Half open");
});
//devreler arası gecişlerde de business kodları mevcut

//circuit breaker (advanced) 4 deger var sondaki deger timera karşılık gelilr yani devrenin ne kadar süre acık kalacagı. diğer 3 tanesi de 1.kısma karşılık geliyor.yani 30 saniye acık kalmadan önce 15 saniye içerisinde min 3 tane hata alırsa ve aynı zamanda bu 15 saniyedeki hatalar da yuzde 50den fazla olursa. yani mesela 10 adet req geldi ve 5 adet hata mesajı olursa ve 3ün de üstünde o zaman devre acık hale gelecek. degerler sisteme göre degişir base practise biz bulacagız.iNTERNETTE HESAPLAMA olayları var ona bak ne kadar reqwde ne olmalı gibi...
var advancedCircuitBreakerPolicy = HttpPolicyExtensions.HandleTransientHttpError().AdvancedCircuitBreakerAsync(failureThreshold:0.5,samplingDuration:TimeSpan.FromSeconds(15),  minimumThroughput: 3, durationOfBreak: TimeSpan.FromSeconds(15), onBreak: (arg1, arg2) =>
{
    Debug.WriteLine("Circuit breaker status => On Break");
}, onReset: () =>
{
    Debug.WriteLine("Circuit breaker status => On Reset");
},
onHalfOpen: () =>
{
    Debug.WriteLine("Circuit breaker status => On Half open");
});
//minimumThroughput:eşik degeri mesela 20 istekten 10 tanesi başarısız ve mindan buyuk oldugu için devreyi acık hale getirir. bu parametreler pollye özgü
//timeout

var timeoutPolicy =Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(1));

//COMBİNE
var combinedPolicy = Policy.WrapAsync(retry, circuitBreakerPolicy, timeoutPolicy);

var bulkheadPolicy = Policy
    .BulkheadAsync<HttpResponseMessage>(maxParallelization: 3, maxQueuingActions: 5);

// 3 istek aynı anda çalışabilir, 5 istek kuyruğa alınır. 8'den fazlası reddedilir.

//await bulkheadPolicy.ExecuteAsync(async () =>
//{
//    Debug.WriteLine("Bulkhead policy");
//});

var fallbackPolicy = Policy<HttpResponseMessage>
    .Handle<HttpRequestException>()
    .FallbackAsync(
        fallbackAction: ct =>
        {
            Console.WriteLine("Fallback triggered!");
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("5")
            });
        });

// Kullanım
//await fallbackPolicy.ExecuteAsync(async () =>
//{
//    return await CallExternalServiceAsync();
//});

builder.Services.AddHttpClient<StockService>(x =>
{
    x.BaseAddress = new Uri("https://localhost:7104/");
}).AddPolicyHandler(combinedPolicy);


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
