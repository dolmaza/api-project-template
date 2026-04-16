using ProjectName.Domain.Common.Mediator.Abstractions;

namespace ProjectName.Domain.Common.Abstractions;

public interface ICommandHandler<in T, TR> : IRequestHandler<T, TR>
    where T : IRequest<TR>
{
}