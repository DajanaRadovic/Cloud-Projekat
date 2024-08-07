﻿using KorisnikService_Data;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace RedditService.Controllers
{
    public class TemaController : Controller
    {
        private readonly SignupDataRepository _userRepository;
        private readonly TemaRepository _temaRepository;
        private readonly KomentarRepository _komentarRepository;
        private readonly PretplataRepository _pretplataRepository;

        public TemaController()
        {
            _userRepository = new SignupDataRepository();
            _temaRepository = new TemaRepository();
            _komentarRepository = new KomentarRepository();
            _pretplataRepository = new PretplataRepository();
        }

        // LOGOUT METODA
        public ActionResult Logout()
        {
            // Ciscenje sesije
            Session["UserID"] = null;
            Session["UserEmail"] = null;
            Session.Clear();
            return RedirectToAction("Index", "Tema");
        }

        // GET: Index
        [HttpGet]
        public ActionResult Index()
        {
            var userEmail = Session["UserEmail"] as string;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Index", "Login");
            }

            var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);
            if (signupDetails == null)
            {
                return HttpNotFound();
            }

            var sveTeme = _temaRepository.GetAllTeme().ToList();
            var userTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList();
            var pretplate = _pretplataRepository.GetPretplateByUserEmail(userEmail).ToList();

            foreach (var tema in sveTeme)
            {
                tema.Komentari = _komentarRepository.GetKomentariByTemaId(tema.RowKey).ToList();
            }

            foreach (var tema in userTeme)
            {
                tema.Komentari = _komentarRepository.GetKomentariByTemaId(tema.RowKey).ToList();
            }

            var model = new EditViewModel
            {
                Signup = signupDetails,
                Teme = sveTeme,
                UserTeme = userTeme,
                Pretplate = pretplate
            };

            return View(model);
        }

        // POST: Index
        [HttpPost]
        public ActionResult Index(EditViewModel model)
        {
            var userEmail = Session["UserEmail"] as string;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Index", "Login");
            }

            if (ModelState.IsValid)
            {
                var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);
                if (signupDetails == null)
                {
                    return HttpNotFound();
                }


                model.Signup.PartitionKey = signupDetails.PartitionKey;
                model.Signup.RowKey = signupDetails.RowKey;

                _userRepository.UpdateSignup(model.Signup, model.ImageFile);
                ViewBag.Message = "Profil je uspešno ažuriran.";
            }


            var sveTeme = _temaRepository.GetAllTeme().ToList();
            var userTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList();
            foreach (var tema in sveTeme)
            {
                tema.Komentari = _komentarRepository.GetKomentariByTemaId(tema.RowKey).ToList();
            }

            foreach (var tema in userTeme)
            {
                tema.Komentari = _komentarRepository.GetKomentariByTemaId(tema.RowKey).ToList();
            }

            model.Teme = sveTeme;
            model.Signup = _userRepository.RetrieveSignupByEmail(userEmail);
            model.UserTeme = userTeme;

            return View("Index", model);
        }

        // POST: AddTema
        [HttpPost]
        public ActionResult AddTema(Tema tema, HttpPostedFileBase imageFile)
        {
            if (ModelState.IsValid)
            {
                var userEmail = Session["UserEmail"] as string;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return RedirectToAction("Index", "Login");
                }

                var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);
                if (signupDetails == null)
                {
                    return HttpNotFound();
                }

                tema.UserEmail = userEmail;
                _temaRepository.AddTema(tema, imageFile);
                ViewBag.LastAddedTemaNaslov = tema.Naslov;


                var sveTeme = _temaRepository.GetAllTeme().ToList();
                var userTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList();


                var model = new EditViewModel
                {
                    Signup = signupDetails,
                    Teme = sveTeme,
                    UserTeme = userTeme
                };

                return View("Index", model);
            }

            return View("AddTema", tema);
        }

        [HttpGet]
        public ActionResult Search(string searchTerm)
        {

            var searchedTemes = _temaRepository.GetAllTeme().Where(t => t.Naslov.Contains(searchTerm)).ToList();

            foreach (var tema in searchedTemes)
            {
                tema.Komentari = _komentarRepository.GetKomentariByTemaId(tema.RowKey).ToList();
            }

            var userEmail = Session["UserEmail"] as string;
            var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);
            var userTeme = _temaRepository.GetTemeByUserEmail(userEmail).Where(t => t.Naslov.Contains(searchTerm)).ToList();

            var model = new EditViewModel
            {
                Signup = signupDetails,
                Teme = searchedTemes,
                UserTeme = userTeme
            };

            return View("Index", model);
        }

        [HttpGet]
        public ActionResult Sort(string sortOrder)
        {
            var userEmail = Session["UserEmail"] as string;
            var allTemes = _temaRepository.GetAllTeme().ToList();
            var userTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList();

            switch (sortOrder)
            {
                case "naslov_asc":
                    allTemes = allTemes.OrderBy(t => t.Naslov).ToList();
                    userTeme = userTeme.OrderBy(t => t.Naslov).ToList();
                    break;
                case "naslov_desc":
                    allTemes = allTemes.OrderByDescending(t => t.Naslov).ToList();
                    userTeme = userTeme.OrderByDescending(t => t.Naslov).ToList();
                    break;
                default:
                    break;
            }

            foreach (var tema in allTemes)
            {
                tema.Komentari = _komentarRepository.GetKomentariByTemaId(tema.RowKey).ToList();
            }

            var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);

            var model = new EditViewModel
            {
                Signup = signupDetails,
                Teme = allTemes,
                UserTeme = userTeme
            };

            return View("Index", model);
        }

        [HttpPost]
        public ActionResult Upvote(string id)
        {
            var userEmail = Session["UserEmail"] as string;
            var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);

            var tema = _temaRepository.GetTemaById(id);

            //  ako je vec upvote temu ne dozvoliti mu da uradi ponovo upvote
            if (tema.EmailKorisnikaUpvote.Contains(userEmail))
            {
                var model_prep = new EditViewModel
                {
                    Signup = signupDetails,
                    Teme = _temaRepository.GetAllTeme().ToList(),
                    UserTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList()
                };
                return RedirectToAction("Index", model_prep);
            }

            if (tema != null)
            {
                tema.Upvotes++; // Povećajte broj upvotova

                // dodaj korisnika u listu email koji su upvote
                tema.EmailKorisnikaUpvote += userEmail + "|";

                // azuriranje teme
                _temaRepository.UpdateTema(tema);
            }

            var model = new EditViewModel
            {
                Signup = signupDetails,
                Teme = _temaRepository.GetAllTeme().ToList(),
                UserTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList()
            };

            return RedirectToAction("Index", model);
        }

        [HttpPost]
        public ActionResult Downvote(string id)
        {
            var userEmail = Session["UserEmail"] as string;
            var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);

            var tema = _temaRepository.GetTemaById(id);

            //  ako je vec downvote temu ne dozvoliti mu da uradi ponovo upvote
            if (tema.EmailKorisnikaDownvote.Contains(userEmail))
            {
                var model_prep = new EditViewModel
                {
                    Signup = signupDetails,
                    Teme = _temaRepository.GetAllTeme().ToList(),
                    UserTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList()
                };
                return RedirectToAction("Index", model_prep);
            }

            if (tema != null)
            {
                tema.Downvotes++; // Povećajte broj downvotova

                // dodaj korisnika u listu email koji su downvote
                tema.EmailKorisnikaDownvote += userEmail + "|";

                _temaRepository.UpdateTema(tema); // Ažuriranje
            }

            var model = new EditViewModel
            {
                Signup = signupDetails,
                Teme = _temaRepository.GetAllTeme().ToList(),
                UserTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList()
            };

            return RedirectToAction("Index", model);
        }

        [HttpPost]
        public ActionResult Delete(string id)
        {
            var userEmail = Session["UserEmail"] as string;
            var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);

            var tema = _temaRepository.GetTemaById(id);

            if (tema == null)
            {
                return HttpNotFound();
            }

            _temaRepository.DeleteTema(tema.PartitionKey, tema.RowKey);
            var userTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList();

            var model = new EditViewModel
            {
                Signup = signupDetails,
                Teme = _temaRepository.GetAllTeme().ToList(),
                UserTeme = userTeme
            };

            return RedirectToAction("Index", model);
        }

        // POST: AddComment
        [HttpPost]
        public async Task<ActionResult> AddComment(string temaId, string komentarTekst)
        {
            var userEmail = Session["UserEmail"] as string;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Index", "Login");
            }

            var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);
            if (signupDetails == null)
            {
                return HttpNotFound();
            }

            var tema = _temaRepository.GetTemaById(temaId);
            if (tema == null)
            {
                return HttpNotFound();
            }

            // Kreiranje novog komentara
            var komentar = new Komentar
            {
                Tekst = komentarTekst,
                KorisnikId = userEmail,
                DatumKreiranja = DateTime.Now,
                TemaId = temaId
            };

            // Dodavanje komentara u bazu podataka
            await _komentarRepository.AddKomentar(komentar);

            // Ažuriranje modela sa novim listom komentara
            tema.Komentari = _komentarRepository.GetKomentariByTemaId(tema.RowKey).ToList();

            // Ažuriranje modela sa novim podacima o korisniku i temama
            var model = new EditViewModel
            {
                Signup = signupDetails,
                Teme = _temaRepository.GetAllTeme().ToList(),
                UserTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList()
            };

            foreach (var t in model.Teme)
            {
                t.Komentari = _komentarRepository.GetKomentariByTemaId(t.RowKey).ToList();
            }

            foreach (var t in model.UserTeme)
            {
                t.Komentari = _komentarRepository.GetKomentariByTemaId(t.RowKey).ToList();
            }

            return View("Index", model);
        }

        [HttpPost]
        public ActionResult DeleteComment(string komentarId)
        {
            var userEmail = Session["UserEmail"] as string;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Index", "Login");
            }

            var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);
            if (signupDetails == null)
            {
                return HttpNotFound();
            }

            var komentar = _komentarRepository.GetKomentarById(komentarId);
            if (komentar == null)
            {
                return HttpNotFound();
            }

            // Brisanje komentara iz baze podataka
            _komentarRepository.DeleteKomentar(komentar.PartitionKey, komentar.RowKey);

            // Ponovno učitavanje tema sa osveženom listom komentara
            var sveTeme = _temaRepository.GetAllTeme().ToList();
            var userTeme = _temaRepository.GetTemeByUserEmail(userEmail).ToList();

            foreach (var tema in sveTeme)
            {
                tema.Komentari = _komentarRepository.GetKomentariByTemaId(tema.RowKey).ToList();
            }

            foreach (var tema in userTeme)
            {
                tema.Komentari = _komentarRepository.GetKomentariByTemaId(tema.RowKey).ToList();
            }

            var model = new EditViewModel
            {
                Signup = signupDetails,
                Teme = sveTeme,
                UserTeme = userTeme
            };

            return RedirectToAction("Index", model);
        }

        [HttpPost]
        public ActionResult Subscribe(string temaId)
        {
            var userEmail = Session["UserEmail"] as string;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Index", "Login");
            }

            var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);
            if (signupDetails == null)
            {
                return HttpNotFound();
            }

            var pretplata = new Pretplata(userEmail, temaId);
            _pretplataRepository.AddPretplata(pretplata);

            return RedirectToAction("Index");
        }

        // POST: Unsubscribe
        [HttpPost]
        public ActionResult Unsubscribe(string temaId)
        {
            var userEmail = Session["UserEmail"] as string;
            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Index", "Login");
            }

            var signupDetails = _userRepository.RetrieveSignupByEmail(userEmail);
            if (signupDetails == null)
            {
                return HttpNotFound();
            }

            _pretplataRepository.DeletePretplata(userEmail, temaId);

            return RedirectToAction("Index");
        }
    }
}
