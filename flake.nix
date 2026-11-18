{
  inputs = {
    nixpkgs.url = "github:NixOS/Nixpkgs/nixos-unstable";
  };
  outputs =
    inputs:
    let
      fsys =
        f:
        inputs.nixpkgs.lib.attrsets.genAttrs [
          "x86_64-linux"
          "armv7l-linux"
          "aarch64-linux"
          "x86_64-darwin"
          "aarch64-darwin"
        ] (s: f s);

    in
    {
      devShells = fsys (
        a:
        let
          pkgs = import inputs.nixpkgs {
            system = a;
          };
        in
        {
          default = pkgs.mkShell {
            packages = [
              pkgs.dotnet-sdk_10
              pkgs.dotnet-aspnetcore_10
              pkgs.sqlite-interactive
            ];
          };
        }
      );
    };
}
