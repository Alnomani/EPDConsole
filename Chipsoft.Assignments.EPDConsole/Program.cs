using Chipsoft.Assignments.EPDConsole.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Chipsoft.Assignments.EPDConsole
{
    public class Program
    {
        //Don't create EF migrations, use the reset db option
        //This deletes and recreates the db, this makes sure all tables exist

        private static void AddPatient()
        {
            Patient newPatient = GetNewPatientInput();
            AddPatientToDb(newPatient);
            Console.WriteLine("Patient toegevoegd. Druk op enter om verder te gaan.");
            Console.ReadLine();
        }

        private static void AddPatientToDb(Patient patientToAdd)
        {
            using (EPDDbContext dbContext = new EPDDbContext())
            {
                dbContext.Database.EnsureCreated();
                dbContext.Patients.Add(patientToAdd);
                dbContext.SaveChanges();
            }
        }
        private static void AddPhysicianToDb(Physician PhysicianToAdd)
        {
            using (EPDDbContext dbContext = new EPDDbContext())
            {
                dbContext.Add(PhysicianToAdd);
                dbContext.SaveChanges();
            }
        }
        private static void AddAppointmentToDb(Appointment appointment)
        {
            using (EPDDbContext dbContext = new EPDDbContext())
            {
                dbContext.Patients.Attach(appointment.AppointmentPatient);
                dbContext.Physicians.Attach(appointment.AppointmentPhysician);
                dbContext.Appointments.Add(appointment);
                dbContext.SaveChanges();
            }
        }

        private static Patient GetNewPatientInput()
        {
            return new Patient()
            {
                Name = GetNameInput("patient"),
                PhoneNumber = GetPhoneNumberInput(),
                Address = GetAdressInput()
            };
        }

        private static Physician GetNewPhysicianInput()
        {
            return new Physician()
            {
                Name = GetNameInput("arts"),
                Address = GetAdressInput()
            };
        }
        private static string GetAdressInput()
        {
            Console.WriteLine("Wat is het address?");
            string? inputAdress = Console.ReadLine();
            while (string.IsNullOrEmpty(inputAdress) || !IsValidAddress(inputAdress))
            {
                Console.WriteLine("Alleen letters, cijfers, punten, comma's of spaties toegestaan.");
                Console.WriteLine("Tenminste 3 characters.");
                inputAdress = Console.ReadLine();
            }

            return inputAdress;
        }
        private static string GetNameInput(string persoonType)
        {
            Console.WriteLine($"Wat is de naam van de {persoonType}?");
            string? inputName = Console.ReadLine();
            while (string.IsNullOrEmpty(inputName) || !IsValidName(inputName))
            {
                Console.WriteLine("Alleen letters in combinatie met spaties toegestaan.");
                inputName = Console.ReadLine();
            }
            return inputName;
        }
        private static string GetPhoneNumberInput()
        {
            Console.WriteLine("Wat is het telefoon nummer van de patient?");
            string? inputName = Console.ReadLine();
            while (string.IsNullOrEmpty(inputName) || !IsValidPhoneNumber(inputName))
            {
                Console.WriteLine("Een telefoon nummer moet 10 cijfers bevatten en geen andere karakters");
                inputName = Console.ReadLine();
            }
            return inputName;
        }
        private static bool IsValidName(string name) => name.All(c => char.IsWhiteSpace(c) || char.IsLetter(c));
        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            Regex r = new Regex(@"\d{10}");
            return r.IsMatch(phoneNumber);

        }
        private static bool IsValidAddress(string address)
        {
            Regex r = new Regex(@"[a-zA-Z0-9,. ]{3,}");
            return r.IsMatch(address);
        }

        private static DateTime GetInputDate()
        {
            Console.WriteLine("Op welke datum wilt u de afspraak inplannen?");
            string? inputDate = Console.ReadLine();
            DateTime? result = GetValidDate(inputDate);
            while (result == null)
            {
                Console.WriteLine("Verkeerd formaat." + 
                     " dd-mm-yy formaat verwacht. Bijvoorbeeld 02-05-2026");
                inputDate = Console.ReadLine();
                result = GetValidDate(inputDate);
            }
            result = AddInputTimeToDate(result);
            if (result < DateTime.Now) 
            {
                Console.WriteLine("Datum al gepasseerd.");
                result = GetInputDate();
            }
              
            return result.Value;
        }

        private static DateTime? AddInputTimeToDate(DateTime? date)
        {
            if(date == null)
                return null;
            string inputTime = GetValidInputTime();
            string[] timeParts = inputTime.Split(":");
            int hours = Int32.Parse(timeParts[0]);
            int minutes = Int32.Parse(timeParts[1]);
            return new DateTime
            (
                date.Value.Year, date.Value.Month, date.Value.Day,
                hours, minutes, 0
            );
        }

        public static string GetValidInputTime()
        {
            Console.WriteLine("Op welk tijdstip wilt u de afspraak inplannen?");
            string? input = Console.ReadLine();
            while (string.IsNullOrEmpty(input) || !IsValidTime(input))
            {
                Console.WriteLine("Verkeerd tijdstip moment. Correct formaat bijvoorbeeld 13:45 .");
                input = Console.ReadLine();
            }
            return input;
        }

        public static bool IsValidTime(string timeInput)
        {
            Regex r = new Regex(@"\d{2}:\d");
            if (!r.IsMatch(timeInput))
                return false;
            string[] timeParts = timeInput.Split(":");
            int hours = Int32.Parse(timeParts[0]);
            int minutes = Int32.Parse(timeParts[1]);
            if (hours < 0 || hours > 23)
                return false;
            if (minutes < 0 || minutes > 59)
                return false;
            return true;
        }

        public static DateTime? GetValidDate(string? inputDate)
        {
            if (string.IsNullOrEmpty(inputDate))
                return null;

            DateTime parsedDate;
            string format = "dd-MM-yyyy";
            IFormatProvider provider = CultureInfo.InvariantCulture;
            DateTimeStyles styles = DateTimeStyles.None;

            if (DateTime.TryParseExact(inputDate, format, provider, styles, out parsedDate))
            {
                return parsedDate;
            }
            else
            {
                Console.WriteLine("Verkeerd datum formaat.");
                return null;
            }
            
        }

        public static Physician? TryGetPhysicianByName(string physicianName)
        {
            using (EPDDbContext dbContext = new EPDDbContext())
            {
                dbContext.Database.EnsureCreated();
                Physician? queriedPhysician = dbContext.Physicians.Where(physician => physician.Name == physicianName).FirstOrDefault();
                if (queriedPhysician == null)
                {
                    Console.WriteLine($"Arts met naam {physicianName} niet gevonden.");
                    return null;
                }
                else
                {
                    Console.WriteLine($"Arts met naam {queriedPhysician.Name}, addres {queriedPhysician.Address} gevonden. ");
                    return queriedPhysician;
                }
            }
        }
        
        public static Patient? TryGetPatientByName(string patientName)
        {
            using (EPDDbContext dbContext = new EPDDbContext())
            {
                dbContext.Database.EnsureCreated();
                Patient? queriedPatient = dbContext.Patients.Where(patient => patient.Name == patientName).FirstOrDefault();
                if (queriedPatient == null)
                {
                    Console.WriteLine($"Patient met naam {patientName} niet gevonden.");
                    return null;
                }
                else
                {
                    Console.WriteLine($"Patient met naam {queriedPatient.Name}, addres {queriedPatient.Address} gevonden. ");
                    return queriedPatient;
                }
            }
        }

        private static void ShowAppointment()
        {
            using (EPDDbContext dbContext = new EPDDbContext())
            {
                dbContext.Database.EnsureCreated();
                List<Appointment> appointments = dbContext.Appointments
                    .Include(appointment => appointment.AppointmentPatient)
                    .Include(appointment => appointment.AppointmentPhysician)
                    .Select(appointment => appointment)
                    .OrderBy(appointment => appointment.AppointmentPatient)
                    .ThenBy(appointment => appointment.AppointmentDate).ToList();
                DisplayAppointments(appointments);
            }
        }
        private static void ShowAppointmentsForPhysician()
        {
            string physicianName = GetNameInput("arts");
            using (EPDDbContext dbContext = new EPDDbContext())
            {
                dbContext.Database.EnsureCreated();
                List<Appointment> appointments = dbContext.Appointments
                    .Include(appointment => appointment.AppointmentPatient)
                    .Include(appointment => appointment.AppointmentPhysician)
                    .Where(appointment => appointment.AppointmentPhysician.Name.Equals(physicianName)).ToList();
                Console.WriteLine($"Afspraken met {physicianName}:");
                DisplayAppointments(appointments);
            }
        }

        private static void DisplayAppointments(List<Appointment> appointments)
        {
            foreach (Appointment appointment in appointments)
            {
                if (appointment.AppointmentPatient != null &&
                    appointment.AppointmentPhysician != null)
                {
                    Console.WriteLine(
                    $"{appointment.AppointmentId}:" +
                    $" Patient {appointment.AppointmentPatient.Name} heeft een afspraak met" +
                    $" arts {appointment.AppointmentPhysician.Name} op {appointment.AppointmentDate}");
                }
            }
            Console.ReadLine();
        }

        private static void ShowAppointmentsForPatient()
        {
            string patientName = GetNameInput("patient");
            using (EPDDbContext dbContext = new EPDDbContext())
            {
                dbContext.Database.EnsureCreated();
                List<Appointment> appointments = dbContext.Appointments
                    .Include(appointment => appointment.AppointmentPatient)
                    .Include(appointment => appointment.AppointmentPhysician)
                    .Where(appointment => appointment.AppointmentPatient.Name.Equals(patientName)).ToList();
                Console.WriteLine($"Afspraken met {patientName}:");
                DisplayAppointments(appointments);
                Console.ReadLine();
            }
        }
        private static void ShowAppointmentsByName()
        {
            Console.WriteLine("Typ 1. voor afspraken van specifieke patienten. " + 
                              "2. Voor afspraken voor specifieke artsen.");
            string? optie = Console.ReadLine();
            if (!string.IsNullOrEmpty(optie) && optie.Equals("1")) 
            {
                ShowAppointmentsForPatient();
            }
            else if(!string.IsNullOrEmpty(optie) && optie.Equals("2")) 
            {
                ShowAppointmentsForPhysician();
            }
        }

        private static void AddAppointment()
        {
            DateTime appointmentTime = GetInputDate();

            string physicianName = GetNameInput("arts");
            Physician? queriedPhysician = TryGetPhysicianByName(physicianName);
            if (queriedPhysician == null)
                return;

            string patientName = GetNameInput("patient");
            Patient? queriedPatient = TryGetPatientByName(patientName);
            if (queriedPatient == null)
                return;

            AddAppointmentToDb(new Appointment()
            {
                AppointmentDate = appointmentTime,
                AppointmentPatient = queriedPatient,
                AppointmentPhysician = queriedPhysician
            });
        }

        private static void DeletePhysician()
        {
            Console.WriteLine($"Arts verwijderen:");
            string physicianName = GetNameInput("arts");
            Physician? queriedPhysician = TryGetPhysicianByName(physicianName);
            if (queriedPhysician != null)
            {
                if (IsConfirmedByUser())
                {
                    DeleteAppointmentsRelatedToPhysicion(physicianName);
                    using (EPDDbContext dbContext = new EPDDbContext())
                    {
                        dbContext.Remove(queriedPhysician);
                        dbContext.SaveChanges();
                    }
                    Console.WriteLine("Arts Verwijderd. Druk op enter om verder te gaan.");
                }
            }
            Console.ReadLine();
        }

        private static void DeleteAppointmentsRelatedToPhysicion(string physicianName)
        {
            using (EPDDbContext dbContext = new EPDDbContext())
            {
                dbContext.Database.EnsureCreated();
                var appointments = dbContext.Appointments
                    .Include(appointment => appointment.AppointmentPatient)
                    .Include(appointment => appointment.AppointmentPhysician)
                    .Where(appointment => appointment.AppointmentPhysician.Name.Equals(physicianName));
                foreach (var appointment in appointments)
                {
                    dbContext.Remove(appointment); 
                }
                dbContext.SaveChanges();
            }
        }
        

        private static void AddPhysician()
        {
            Physician newPhysician = GetNewPhysicianInput();
            AddPhysicianToDb(newPhysician);
            Console.WriteLine("Arts toegevoegd. Druk op enter om verder te gaan.");
            Console.ReadLine();
        }

        private static void DeletePatient()
        {
            Console.WriteLine($"Patient verwijderen:");
            string patientName = GetNameInput("patient");
            Patient? queriedPatient = TryGetPatientByName(patientName);
            if (queriedPatient != null)
            {
                if (IsConfirmedByUser())
                {
                    DeleteAppointmentsRelatedToPatient(patientName);
                    using (EPDDbContext dbContext = new EPDDbContext())
                    {
                        dbContext.Remove(queriedPatient);
                        dbContext.SaveChanges();
                    }
                    Console.WriteLine("Patient Verwijderd. Druk op enter om verder te gaan.");
                }
            }
            Console.ReadLine();
        }

        private static void DeleteAppointmentsRelatedToPatient(string patientName)
        {
            using (EPDDbContext dbContext = new EPDDbContext())
            {
                dbContext.Database.EnsureCreated();
                var appointments = dbContext.Appointments
                    .Include(appointment => appointment.AppointmentPatient)
                    .Include(appointment => appointment.AppointmentPhysician)
                    .Where(appointment => appointment.AppointmentPatient.Name.Equals(patientName));
                foreach (var appointment in appointments)
                {
                    dbContext.Remove(appointment);
                }
                dbContext.SaveChanges();
            }
        }

        private static bool IsConfirmedByUser()
        {
            Console.WriteLine("Weet u zeker dat u hier mee door wil gaan. Typ de letter \"j\" ter bevestiging.");
            string? confirmation = Console.ReadLine();
            if (!string.IsNullOrEmpty(confirmation) && confirmation.Equals("j"))
                return true;
            return false;
        }

        #region FreeCodeForAssignment
        static void Main(string[] args)
        {
            while (ShowMenu())
            {
                //Continue
            }
        }

        public static bool ShowMenu()
        {
            Console.Clear();
            foreach (var line in File.ReadAllLines("logo.txt"))
            {
                Console.WriteLine(line);
            }
            Console.WriteLine("");
            Console.WriteLine("1 - Patient toevoegen");
            Console.WriteLine("2 - Patienten verwijderen");
            Console.WriteLine("3 - Arts toevoegen");
            Console.WriteLine("4 - Arts verwijderen");
            Console.WriteLine("5 - Afspraak toevoegen");
            Console.WriteLine("6 - Afspraken inzien");
            Console.WriteLine("7 - Sluiten");
            Console.WriteLine("8 - Reset db");
            Console.WriteLine("9 - Afpraken inzien van specifiek persoon");

            if (int.TryParse(Console.ReadLine(), out int option))
            {
                switch (option)
                {
                    case 1:
                        AddPatient();
                        return true;
                    case 2:
                        DeletePatient();
                        return true;
                    case 3:
                        AddPhysician();
                        return true;
                    case 4:
                        DeletePhysician();
                        return true;
                    case 5:
                        AddAppointment();
                        return true;
                    case 6:
                        ShowAppointment();
                        return true;
                    case 7:
                        return false;
                    case 8:
                        EPDDbContext dbContext = new EPDDbContext();
                        dbContext.Database.EnsureDeleted();
                        dbContext.Database.EnsureCreated();
                        return true;
                    case 9:
                        ShowAppointmentsByName();
                        return true;
                    default:
                        return true;
                }
            }
            return true;
        }

        #endregion
    }
}