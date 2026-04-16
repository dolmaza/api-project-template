using ProjectName.Domain.Common.Mediator.Abstractions;

namespace ProjectName.Domain.Common.Abstractions;

public interface ICommand<TResponse> : IRequest<TResponse>
{

}