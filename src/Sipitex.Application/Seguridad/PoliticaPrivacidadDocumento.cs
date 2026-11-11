using System.Text;

namespace Sipitex.Application.Seguridad;

public sealed record PoliticaEnlace(string Texto, string Url);

public sealed class PoliticaBloque
{
    public string? Titulo { get; init; }

    public IReadOnlyList<string> Parrafos { get; init; } = [];

    public IReadOnlyList<string> Items { get; init; } = [];

    public IReadOnlyList<PoliticaEnlace> Enlaces { get; init; } = [];
}

public sealed class PoliticaDocumento
{
    public required string Version { get; init; }

    public required string Fecha { get; init; }

    public required IReadOnlyList<PoliticaBloque> Bloques { get; init; }

    public string ATexto()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Política de tratamiento de datos personales — SIPITEX");
        sb.AppendLine($"Versión {Version} · Actualizada: {Fecha}");
        sb.AppendLine();
        foreach (var bloque in Bloques)
        {
            if (!string.IsNullOrWhiteSpace(bloque.Titulo))
            {
                sb.AppendLine(bloque.Titulo);
                sb.AppendLine();
            }

            foreach (var parrafo in bloque.Parrafos)
            {
                sb.AppendLine(parrafo);
                sb.AppendLine();
            }

            foreach (var item in bloque.Items)
                sb.AppendLine("- " + item);
            if (bloque.Items.Count > 0)
                sb.AppendLine();

            foreach (var enlace in bloque.Enlaces)
            {
                sb.AppendLine($"{enlace.Texto}: {enlace.Url}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}

// Texto único de la política. La vista y la descarga usan esto, para no divergir.
public static class PoliticaPrivacidadDocumento
{
    public static PoliticaDocumento Crear(PrivacidadOptions opciones, string? version = null, int minutosInactividad = 20)
    {
        var pedida = string.IsNullOrWhiteSpace(version) ? opciones.VersionVigente() : version.Trim();
        if (string.Equals(pedida, PoliticaPrivacidad.VersionAnterior, StringComparison.Ordinal))
            return Archivo10(opciones);

        var minutos = minutosInactividad < 2 ? 2 : minutosInactividad;
        return Actual(opciones, minutos);
    }

    private static PoliticaDocumento Actual(PrivacidadOptions opciones, int minutosInactividad)
    {
        var bloques = new List<PoliticaBloque>
        {
            Introduccion(opciones),
            Definiciones(),
            Responsable(opciones),
            Datos(minutosInactividad),
            Menores(),
            Finalidades(),
            BaseLegal(),
            Compartir(opciones),
            Conservacion(opciones),
            Derechos(),
            Plazos(opciones),
            Revocatoria(),
            Seguridad(minutosInactividad),
            Cookies(),
            Versiones(opciones),
            Rnbd()
        };

        return new PoliticaDocumento
        {
            Version = opciones.VersionVigente(),
            Fecha = opciones.FechaPublica(),
            Bloques = bloques
        };
    }

    private static PoliticaBloque Introduccion(PrivacidadOptions opciones)
    {
        var parrafos = new List<string>
        {
            "Esta política aplica a SIPITEX y a todo dato personal que se trate en él. SIPITEX es el sistema de aprendizaje, producción e inventario textil del Centro de Manufactura en Textil y Cuero del SENA. Está dirigida a aprendices, instructores y demás personas que usan el sistema.",
            "El tratamiento se informa en los términos de la Ley 1581 de 2012, del Decreto 1074 de 2015 (Único Reglamentario del sector Comercio, que compila el Decreto 1377 de 2013) y de las demás normas que las modifiquen o complementen."
        };
        var enlaces = new List<PoliticaEnlace>();
        var url = PrivacidadConfiguracion.UrlPublica(opciones.UrlPoliticaInstitucional);
        if (url is not null)
            enlaces.Add(new PoliticaEnlace("Política institucional de datos del SENA", url));

        return new PoliticaBloque
        {
            Titulo = "Introducción y alcance",
            Parrafos = parrafos,
            Enlaces = enlaces
        };
    }

    private static PoliticaBloque Definiciones() => new()
    {
        Titulo = "Definiciones",
        Items =
        [
            "Dato personal: información que identifica o puede identificar a una persona.",
            "Titular: la persona a quien se refieren los datos, por ejemplo un aprendiz o un instructor.",
            "Responsable: quien decide para qué y cómo se tratan los datos. Aquí es el SENA, a través del Centro de Manufactura en Textil y Cuero.",
            "Encargado: quien trata datos por cuenta del responsable, por ejemplo quien aloja el sistema o envía el correo.",
            "Tratamiento: cualquier operación sobre los datos, como recolectarlos, guardarlos, usarlos, actualizarlos o suprimirlos.",
            "Autorización: el consentimiento previo, expreso e informado del titular.",
            "Dato sensible: el que afecta la intimidad o cuyo uso indebido puede discriminar, como la salud, el origen racial, la convicción religiosa, la vida sexual o la biometría.",
            "Transferencia: enviar datos a un tercero para que decida sobre ellos como responsable.",
            "Transmisión: enviar datos a un encargado para que los trate por cuenta del responsable."
        ]
    };

    private static PoliticaBloque Responsable(PrivacidadOptions opciones)
    {
        var parrafos = new List<string>();
        var nombre = Limpio(opciones.ResponsableNombre);
        parrafos.Add(nombre
            ?? "El responsable del tratamiento es el SENA, por medio del Centro de Manufactura en Textil y Cuero (CMTC).");
        Agregar(parrafos, "NIT: ", opciones.NIT);
        Agregar(parrafos, "Ciudad: ", opciones.Ciudad);
        Agregar(parrafos, "Correo de contacto: ", opciones.CorreoContacto);
        Agregar(parrafos, "Dirección de notificación: ", opciones.Direccion);
        Agregar(parrafos, "Teléfono de contacto: ", opciones.Telefono);
        Agregar(parrafos, "Horario de atención: ", opciones.HorarioAtencion);
        var area = Limpio(opciones.AreaSolicitudes);
        if (area is not null)
            parrafos.Add("Las solicitudes sobre datos personales las atiende: " + area + ".");

        parrafos.Add(
            "Quien administra el alojamiento del sistema o la base de datos, y el proveedor que envía el correo, actúan como encargados del tratamiento. Solo pueden usar los datos para prestar ese servicio, bajo el contrato o el acuerdo de transmisión de datos correspondiente.");

        return new PoliticaBloque
        {
            Titulo = "Responsable del tratamiento",
            Parrafos = parrafos
        };
    }

    private static PoliticaBloque Datos(int minutosInactividad) => new()
    {
        Titulo = "Datos que se recopilan",
        Parrafos =
        [
            "SIPITEX no pide datos sensibles. La foto de perfil, si usted la carga, se muestra en la cuenta y no se usa para reconocimiento facial. Si algún día se usara para eso, sería un dato biométrico y haría falta una autorización expresa nueva.",
            "No se guarda la contraseña en texto legible: solo un hash. No se guarda el código de verificación en texto legible en la tabla de códigos, en la auditoría ni en los logs: solo se guarda su hash. El código viaja en el correo. Si el envío se deja en el archivo interno de mensajes, en lugar de entregarlo a un proveedor, el texto de ese mensaje queda en la base. El canal de producción no archiva el mensaje."
        ],
        Items =
        [
            "Identificación y cuenta: nombre y correo.",
            "Rol, ficha asignada y plantas de inventario.",
            "Descripción de funciones y foto de perfil. Son opcionales y solo se guardan si usted las carga.",
            "Auditoría del taller: quién hizo un cambio, sobre qué registro y cuándo.",
            "Prueba de la autorización: versión de esta política que aceptó, fecha en hora UTC y dirección IP de esa aceptación.",
            "Cookie de sesión (sipitex.auth), que identifica la cuenta, el rol y la versión de la política aceptada.",
            "Cookie de protección de formularios (sipitex.antiforgery).",
            "Correo y dirección IP de los intentos de ingreso, solo en la memoria del servidor, para bloquear el acceso tras 5 fallos durante 15 minutos. Ese bloqueo no se escribe en la base y se pierde al reiniciar el proceso.",
            $"Marca de tiempo de la actividad, para cerrar la sesión tras {minutosInactividad} minutos sin uso."
        ]
    };

    private static PoliticaBloque Menores() => new()
    {
        Titulo = "Menores de edad",
        Parrafos =
        [
            "Si el titular es menor de 18 años, el tratamiento se limita a los datos necesarios para la formación y prevalece el interés superior del menor. Se requiere la autorización de su representante legal.",
            "El registro de SIPITEX no pide la fecha de nacimiento ni un campo para la autorización del representante. Cuando se sepa que el titular es menor de edad, esa autorización debe gestionarse por fuera del sistema hasta que el centro habilite la verificación."
        ]
    };

    private static PoliticaBloque Finalidades() => new()
    {
        Titulo = "Finalidades",
        Parrafos =
        [
            "No se usan los datos para publicidad ni para finalidades distintas de las siguientes sin una autorización nueva."
        ],
        Items =
        [
            "Gestionar el taller: órdenes, materiales, inventario y reportes.",
            "Controlar el acceso según el rol.",
            "Dejar trazabilidad de los cambios por seguridad.",
            "Enviar códigos de verificación y avisos del sistema.",
            "Cumplir obligaciones de formación, inventario y auditoría del centro."
        ]
    };

    private static PoliticaBloque BaseLegal() => new()
    {
        Titulo = "Base legal y autorización",
        Parrafos =
        [
            "La autorización es previa, expresa e informada. Usted la da al marcar una casilla que no viene marcada, con enlace a esta política, cuando confirma el correo o cuando ingresa de nuevo porque la versión cambió.",
            "De cada aceptación se guarda la versión, la fecha en hora UTC y la dirección IP. Las aceptaciones anteriores no se borran.",
            "La ley prevé casos en los que se pueden tratar datos sin autorización. El centro solo aplicará uno de esos casos cuando el área jurídica confirme que corresponde a este sistema."
        ]
    };

    private static PoliticaBloque Compartir(PrivacidadOptions opciones)
    {
        var parrafos = new List<string>
        {
            "Los datos se comparten con el proveedor de correo que envía los códigos de verificación y los avisos, y con quien aloja la aplicación y la base de datos. Esas personas o empresas actúan como encargadas del tratamiento.",
            "También pueden conocerlos las autoridades que los pidan con fundamento legal.",
            "No se venden ni se ceden para publicidad."
        };

        var internacionales = new List<string>();
        AgregarProveedor(parrafos, internacionales, "Proveedor de correo", opciones.ProveedorCorreo, opciones.PaisProveedorCorreo);
        AgregarProveedor(parrafos, internacionales, "Proveedor de hosting y base de datos", opciones.ProveedorHosting, opciones.PaisProveedorHosting);
        if (internacionales.Count > 0)
        {
            parrafos.Add(
                "Hay transmisión o transferencia internacional con " +
                string.Join("; ", internacionales) +
                ". Se hace bajo las garantías que exige la ley.");
        }

        return new PoliticaBloque
        {
            Titulo = "Con quién se comparten los datos",
            Parrafos = parrafos
        };
    }

    private static PoliticaBloque Conservacion(PrivacidadOptions opciones)
    {
        var parrafos = new List<string>
        {
            "Los datos se conservan mientras la cuenta esté activa y, después, durante el tiempo que exijan los registros de formación, inventario y auditoría del centro."
        };
        var plazo = Limpio(opciones.PlazoConservacion);
        if (plazo is not null)
            parrafos.Add("El plazo definido por el centro, contado desde la terminación de la formación o de la vinculación, es: " + plazo + ".");

        parrafos.Add("Cumplido ese tiempo, se suprimen o se anonimizan.");
        parrafos.Add("Los registros de auditoría pueden conservarse más tiempo cuando exista un deber legal o de seguridad.");
        return new PoliticaBloque
        {
            Titulo = "Conservación y supresión",
            Parrafos = parrafos
        };
    }

    private static PoliticaBloque Derechos() => new()
    {
        Titulo = "Derechos del titular",
        Items =
        [
            "Conocer, actualizar y rectificar sus datos.",
            "Solicitar prueba de la autorización.",
            "Ser informado del uso que se les da.",
            "Presentar quejas ante la Superintendencia de Industria y Comercio (SIC), después de agotar el trámite con el responsable.",
            "Revocar la autorización y solicitar la supresión, salvo cuando exista un deber legal o contractual de conservarlos.",
            "Acceder de forma gratuita a sus datos."
        ]
    };

    private static PoliticaBloque Plazos(PrivacidadOptions opciones)
    {
        var canal = Limpio(opciones.CorreoContacto) is null
            ? "Puede usar la sección Mis datos en Mi perfil, si ya tiene cuenta."
            : "Puede escribir al correo del responsable publicado en esta política, o usar la sección Mis datos en Mi perfil si ya tiene cuenta.";
        return new PoliticaBloque
        {
            Titulo = "Cómo ejercer los derechos y plazos",
            Parrafos =
            [
                canal + " Toda solicitud queda en la auditoría del sistema, con la fecha, el tipo y el estado.",
                "Las consultas se responden en máximo 10 días hábiles, prorrogables 5 más si se le avisan los motivos.",
                "Los reclamos, incluidas la rectificación, la supresión y la revocatoria, se responden en máximo 15 días hábiles, prorrogables 8 más si se le avisan los motivos. Si el reclamo está incompleto, se pide subsanarlo en 5 días. Si pasan 2 meses sin respuesta del titular, se entiende desistido.",
                "Consultar los datos es gratuito.",
                "El titular solo puede acudir a la Superintendencia de Industria y Comercio después de haber agotado este trámite con el responsable."
            ]
        };
    }

    private static PoliticaBloque Revocatoria() => new()
    {
        Titulo = "Efectos de revocar la autorización",
        Parrafos =
        [
            "Revocar la autorización puede impedir seguir usando SIPITEX, porque el rol y el correo son necesarios para entrar y para dejar trazabilidad de lo que usted hace en el taller.",
            "La revocatoria no procede cuando exista un deber legal o contractual de conservar los datos."
        ]
    };

    private static PoliticaBloque Seguridad(int minutosInactividad) => new()
    {
        Titulo = "Seguridad",
        Items =
        [
            "El acceso a cada función depende del rol (administrador, instructor o encargado de bodega) y de los permisos que asigne el administrador.",
            "El ingreso exige correo y contraseña. La contraseña se guarda con un hash PBKDF2 y no en texto legible.",
            "La cuenta nueva confirma el correo con un código de 6 dígitos, de un solo uso, que vence a los 15 minutos. El mismo mecanismo sirve para restablecer la contraseña. En la tabla de códigos solo queda el hash SHA-256. El código y la contraseña no se escriben en la auditoría ni en los logs.",
            "En producción el sitio redirige a HTTPS y envía HSTS por un año, incluidos los subdominios.",
            "La cookie de sesión es HttpOnly y SameSite Lax. Cuando la conexión es HTTPS, además es Secure.",
            $"La sesión se cierra tras {minutosInactividad} minutos sin actividad.",
            "Los formularios exigen un token antifalsificación.",
            "El servidor envía cabeceras para que el navegador no adivine el tipo de archivo, no cargue la página dentro de otro sitio y no use cámara, micrófono ni ubicación. También hay una política de contenido y de referentes.",
            "Tras 5 intentos de ingreso fallidos con el mismo correo y la misma dirección IP, el ingreso se bloquea 15 minutos. El mensaje no indica si el correo existe.",
            "La auditoría del taller guarda quién cambió un registro, cuál fue y cuándo. No guarda la contraseña ni el código de verificación."
        ]
    };

    private static PoliticaBloque Cookies() => new()
    {
        Titulo = "Cookies y analítica",
        Parrafos =
        [
            "SIPITEX no usa analítica de terceros ni cookies de publicidad.",
            "Las pantallas cargan la tipografía desde fonts.googleapis.com y fonts.gstatic.com, y los iconos desde cdnjs.cloudflare.com. Estadísticas carga gráficos y trazabilidad carga el generador de códigos QR desde cdn.jsdelivr.net. Esas cargas no son analítica: SIPITEX no les envía el nombre, el correo ni el rol. El navegador sí se conecta a esos sitios para traer el archivo."
        ],
        Items =
        [
            "sipitex.auth: cookie de sesión y autenticación. Identifica la cuenta mientras la sesión está abierta.",
            "sipitex.antiforgery: cookie de seguridad de los formularios. No sirve para seguir la navegación."
        ]
    };

    private static PoliticaBloque Versiones(PrivacidadOptions opciones) => new()
    {
        Titulo = "Cambios a la política y control de versiones",
        Parrafos =
        [
            "Cada cambio sustancial sube la versión y exige una nueva aceptación en el siguiente ingreso. La casilla no viene marcada. La aceptación anterior se conserva.",
            $"La versión vigente de este texto es {opciones.VersionVigente()} ({opciones.FechaPublica()}).",
            $"Historial: {PoliticaPrivacidad.VersionAnterior} ({PoliticaPrivacidad.FechaVersionAnterior}), {PoliticaPrivacidad.Version} ({PoliticaPrivacidad.FechaActualizacion})."
        ]
    };

    private static PoliticaBloque Rnbd() => new()
    {
        Titulo = "Registro Nacional de Bases de Datos",
        Parrafos =
        [
            "El Registro Nacional de Bases de Datos lo administra la Superintendencia de Industria y Comercio. El centro debe confirmar con el área jurídica si la base de este sistema tiene que inscribirse. Esta política no afirma que la base esté inscrita ni publica un número de registro."
        ]
    };

    private static PoliticaDocumento Archivo10(PrivacidadOptions opciones)
    {
        var responsable = new List<string>();
        var nombre = Limpio(opciones.ResponsableNombre);
        if (nombre is not null)
            responsable.Add(nombre);
        Agregar(responsable, "Correo de contacto: ", opciones.CorreoContacto);
        Agregar(responsable, "Dirección de notificación: ", opciones.Direccion);
        Agregar(responsable, "Teléfono de contacto: ", opciones.Telefono);
        Agregar(responsable, "Ciudad: ", opciones.Ciudad);
        if (responsable.Count == 0)
            responsable.Add("El responsable es el Centro de Manufactura en Textil y Cuero del SENA.");

        return new PoliticaDocumento
        {
            Version = PoliticaPrivacidad.VersionAnterior,
            Fecha = PoliticaPrivacidad.FechaVersionAnterior,
            Bloques =
            [
                new PoliticaBloque
                {
                    Titulo = "Versión 1.0",
                    Parrafos =
                    [
                        "Texto conservado de la versión 1.0 para consulta. La versión vigente es posterior.",
                        "Esta política aplica a SIPITEX, el sistema de aprendizaje, producción e inventario textil del Centro de Manufactura en Textil y Cuero. Se informa en los términos de la Ley 1581 de 2012 y el Decreto 1377 de 2013."
                    ]
                },
                new PoliticaBloque
                {
                    Titulo = "Responsable del tratamiento",
                    Parrafos = responsable
                },
                new PoliticaBloque
                {
                    Titulo = "Qué datos se recopilan",
                    Items =
                    [
                        "Nombre y correo, para identificar la cuenta.",
                        "Rol, ficha asignada y plantas de inventario.",
                        "Descripción de funciones y foto de perfil, si usted las carga.",
                        "Actividad de auditoría: quién hizo un cambio, sobre qué registro y cuándo. No se guarda la contraseña ni los códigos de verificación.",
                        "La versión de esta política que aceptó, la fecha en hora UTC y la dirección IP de esa aceptación."
                    ]
                },
                new PoliticaBloque
                {
                    Titulo = "Para qué se usan",
                    Parrafos =
                    [
                        "Gestionar el taller (órdenes, materiales, inventario y reportes), controlar el acceso según el rol y dejar trazabilidad de los cambios por seguridad. La base es la autorización que usted otorga al aceptar esta política y la relación de formación o trabajo con el centro."
                    ]
                },
                new PoliticaBloque
                {
                    Titulo = "Cuánto tiempo se conservan",
                    Parrafos =
                    [
                        "Mientras la cuenta esté activa y, después, el tiempo que exijan los registros de formación, inventario y auditoría del centro. Cuando ya no hagan falta, se suprimen o se anonimizan."
                    ]
                },
                new PoliticaBloque
                {
                    Titulo = "Con quién se comparten",
                    Parrafos =
                    [
                        "Con el proveedor de correo que envía códigos y avisos, y con el proveedor de hosting donde corre la aplicación y la base de datos. No se venden ni se ceden a terceros para publicidad."
                    ]
                },
                new PoliticaBloque
                {
                    Titulo = "Derechos del titular",
                    Parrafos =
                    [
                        "Usted puede conocer, actualizar, rectificar y suprimir sus datos, revocar la autorización y presentar quejas ante la Superintendencia de Industria y Comercio (SIC).",
                        "Para ejercerlos, escriba al correo del responsable o, si ya tiene cuenta, use la sección Mis datos en Mi perfil. La solicitud queda en la auditoría del sistema.",
                        "Revocar la autorización puede impedir seguir usando SIPITEX, porque el rol y el correo son necesarios para entrar."
                    ]
                }
            ]
        };
    }

    private static void AgregarProveedor(
        List<string> parrafos,
        List<string> internacionales,
        string rol,
        string? nombre,
        string? pais)
    {
        var n = Limpio(nombre);
        var p = Limpio(pais);
        if (n is null && p is null)
            return;

        if (n is not null && p is not null)
            parrafos.Add($"{rol}: {n}. País: {p}.");
        else if (n is not null)
            parrafos.Add($"{rol}: {n}.");
        else
            parrafos.Add($"{rol}. País: {p}.");

        if (p is not null && !EsColombia(p))
            internacionales.Add($"{n ?? rol}, {p}");
    }

    private static bool EsColombia(string pais)
    {
        var n = pais.Trim();
        return n.Equals("Colombia", StringComparison.OrdinalIgnoreCase)
            || n.Equals("CO", StringComparison.OrdinalIgnoreCase)
            || n.Contains("Colombia", StringComparison.OrdinalIgnoreCase);
    }

    private static void Agregar(List<string> parrafos, string etiqueta, string? valor)
    {
        var limpio = Limpio(valor);
        if (limpio is not null)
            parrafos.Add(etiqueta + limpio);
    }

    private static string? Limpio(string? valor) => PrivacidadConfiguracion.ValorPublico(valor);
}
