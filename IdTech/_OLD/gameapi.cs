using Quake2Sharp.game.types;

namespace Quake2Sharp;

public class game_export_t
{
}

public interface IGameApi
{
	game_export_t GetGameAPI(game_import_t parms);
}
