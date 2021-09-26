namespace DaprSample.MicroService.UsersService.Datas.Dto
{
    public class CommonResponse<T>
    {
        public int ErrorCode { get; set; }
 
        public T Data { get; set; }
    }
}
