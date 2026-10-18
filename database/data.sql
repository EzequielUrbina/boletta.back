set datestyle to 'European';

insert into config.role(name, description)
values ('admin', 'Full system access.'),
       ('secretary', 'Access to the secretary and academy modules.'),
       ('cashier', 'Access to the accounting module.'),
       ('teacher', 'Access to the academic module.'),
       ('principal', 'Access to the management module.'),
       ('inspector', 'Access to the student incident module.');

insert into config.user(roleid, name, secondname, surname, secondsurname, email, userstate, createdat, updatedat, password)
values (4, 'Docente', 'por', 'Defecto', 'del sistema', 'user.default@boletta.app', true, now(), now(),'AQAAAAIAAYagAAAAEBA+otefABAFYU//4mkRSCB+4Ehre7sDid871rFP7vW3snwji5+cxvjXsWUa1AasZw=='),
       (1, 'Ezequiel', 'De jesús', 'Urbina', 'Zeledón', 'ezequiel.urbina@boletta.app', true, now(), now(),'AQAAAAIAAYagAAAAEK/ObbY+PMQMXK/Q2rqJQyZKPUwiZGPALh/Bww0t6j9gozilS/PVoYQfLo8eDoHmFA==');


insert into config.permission(name, spanishname, area, description)
values ('student:create', 'Crear estudiantes', 'secretary','Permission for the creation of students in the secretary scheme.'),
       ('student:update', 'Modificar estudiantes', 'secretary','Permission for the update of students in the secretary scheme.'),
       ('student:read', 'Ver estudiantes', 'secretary','Permission for the reading of students in the secretary scheme.'),
       ('student:enroll', 'Matricular estudiantes', 'secretary','Permission for enroll of students in the secretary scheme.'),
       ('user:create', 'Crear perfiles de usuarios', 'config', 'Permission for creation user profiles.'),
       ('user:update', 'Modificar perfiles de usuarios', 'config', 'Permission for update user profiles.'),
       ('user:read', 'Ver perfiles de usuarios', 'config', 'Permission for reading user profiles.'),
       ('rol:read', 'Ver roles', 'config', 'Permission for reading roles.'),
       ('rol:update', 'Modificar roles', 'config', 'Permission for update roles.'),
       ('permission:read', 'Ver permisos', 'config', 'Permission for reading permissions.'),
       ('partial:update', 'Modificar parciales', 'academy', 'Permission for update partials.'),
       ('partial:read', 'Ver parciales', 'academy', 'Permission for reading partials.'),
       ('report:read', 'Ver reportes', 'academy', 'Permission for reading report.'),
       ('transaction:create', 'Crear transacciones', 'accounting', 'Permission for creation transactions.'),
       ('transaction:update', 'Modificar transacciones', 'accounting', 'Permission for update transactions.'),
       ('transaction:read', 'Ver transacciones', 'accounting', 'Permission for reading transactions.'),
       ('tariff:update', 'Modificar tarifas', 'accounting', 'Permission for update tariffs.'),
       ('tariff:read', 'Ver tarifas', 'accounting', 'Permission for reading tariffs.'),
       ('enrollment:create', 'Crear matrículas', 'academy', 'Permission for creation enrollments.'),
       ('enrollment:update', 'Modificar matrículas', 'academy', 'Permission for update enrollments.'),
       ('enrollment:read', 'Ver matrículas', 'academy', 'Permission for reading enrollments.'),
       ('degree:read', 'Ver grados', 'academy', 'Permission for reading degrees.'),
       ('debt:update', 'Modificar deudas', 'accounting', 'Permission for update debt.'),
       ('debt:read', 'Ver deudas', 'accounting', 'Permission for reading debt.'),
       ('teacher:read', 'Ver docentes', 'academy', 'Permission for reading teachers.'),
       ('grade:update', 'Modificar calificaciones', 'academy', 'Permission for update grades.'),
       ('register:read', 'Ver padrón', 'secretary', 'Permission for reading student register.'),
       ('teacher:enrollment:guide', 'Ver sección guidada', 'academy', 'Permission for reading enrollment guide (teacher).'),
       ('schoolyear:create', 'Crear año lectivos', 'secretary', 'Permission for creations schoolyears.'),
       ('schoolyear:update', 'Modificar año lectivos', 'secretary', 'Permission for update schoolyears.'),
       ('schoolyear:read', 'Ver año lectivos', 'secretary', 'Permission for reading schoolyears.'),
       ('catalog:create', 'Crear catalogos', 'secretary', 'Permission for creations catalogs.'),
       ('catalog:update', 'Modificar catalogos', 'secretary', 'Permission for update catalogs.'),
       ('catalog:read', 'Ver catalogos', 'secretary', 'Permission for reading catalogs.'),
       ('report:principal:read', 'Ver reporte de director', 'secretary', 'Permission for reading principal report.'),
       ('report:teacher:read', 'Ver reporte de docente', 'academy', 'Permission for reading teacher report.');


-- Admin --
-- There are permissions that do not correspond to this role, they are temporary --
INSERT INTO config.role_permission(roleid, permissionid)
SELECT 1, p.permissionid FROM config.permission p;

-- Secretary --
INSERT INTO config.role_permission(roleid, permissionid)
SELECT 2, p.permissionid FROM config.permission p
WHERE p.name in ('student:create', 'student:read', 'student:update', 'user:read', 'report:read', 'enrollment:create',
                 'enrollment:update','enrollment:read','teacher:read', 'register:read', 'schoolyear:create',
                 'schoolyear:update', 'schoolyear:read');

-- Cashier --
INSERT INTO config.role_permission(roleid, permissionid)
SELECT 3, p.permissionid FROM config.permission p
WHERE p.name in ('student:create', 'student:read', 'student:update', 'user:read', 'report:read', 'transaction:create',
                 'transaction:update', 'transaction:read', 'tariff:update', 'tariff:read', 'debt:update', 'debt:read');

-- Teacher --
INSERT INTO config.role_permission(roleid, permissionid)
SELECT 4, p.permissionid FROM config.permission p
WHERE p.name in ('user:read', 'partial:read', 'degree:read', 'teacher:read', 'grade:update', 'teacher:enrollment:guide', 'report:teacher:read');

-- Principal --
INSERT INTO config.role_permission(roleid, permissionid)
SELECT 5, p.permissionid FROM config.permission p
WHERE p.name in ('user:read', 'partial:read', 'partial:update', 'student:read', 'report:read', 'register:read', 'report:principal:read');


-- ############################## ---
insert into accounting.discount(discountid, description, tag)
values (1, 'Sin descuento', 'Sin descuento'),
       (2, 'Descuento por hijos', 'Descuento básico'),
       (3, 'Descuento por hijos del personal', 'Descuento trabajadores');

insert into accounting.discounteducationallevel(del, discountId, educationalLevel, amount)
values (1, 1, 1, 0),
       (2, 1, 2, 0),
       (3, 1, 3, 0),
       (4, 2, 1, 0.08),
       (5, 2, 2, 0.08),
       (6, 2, 3, 0.08),
       (7, 3, 1, 0.5),
       (8, 3, 2, 0.5),
       (9, 3, 3, 0.5);

insert into accounting.tarifftype(description)
values ('Mensualidad'),
       ('Matrícula'),
       ('Útiles'),
       ('Otros');


INSERT INTO Accounting.cashier(userid)
SELECT u.userid FROM config.user u WHERE u.name = 'Ezequiel';

INSERT INTO Academy.teacher(userid, isguide)
SELECT u.userid, false
FROM config.user u
WHERE u.name = 'Docente';