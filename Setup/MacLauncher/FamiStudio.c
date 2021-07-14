#include <mono/jit/jit.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-config.h>

int main(int argc, char *argv[])
{
    MonoDomain*   domain   = NULL;
    MonoAssembly* assembly = NULL;

    if (argc < 2)
    {
        printf("Missing assembly name.\n");
        return -1;
    }

    int retval;
    
    mono_config_parse (NULL);

    domain = mono_jit_init("FamiStudio");

    assembly = mono_domain_assembly_open(domain, argv[1]);

    if (!assembly)
    {   
        printf("Error opening assembly '%s'.\n", argv[1]);
        return -1;
    }
    
    retval = mono_jit_exec(domain, assembly, argc - 1, argv + 1);

    mono_jit_cleanup(domain);

    return 0;
}
