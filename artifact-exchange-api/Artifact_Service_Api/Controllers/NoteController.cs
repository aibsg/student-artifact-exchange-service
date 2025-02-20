using Microsoft.AspNetCore.Mvc;
using Artifact_Service_Api.Models;
using Artifact_Service_Api.AppData;
using Artifact_Service_Api.Dtos;
using Microsoft.EntityFrameworkCore;
using System.Xml.XPath;

namespace Artifact_Service_Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class NoteController(AppDbContext context) : ControllerBase
{
    private readonly AppDbContext _context = context;
    
    [HttpGet]
    public async Task<IEnumerable<Note>> GetOpenNotes() =>
        await _context.Notes
            .Where(n => n.IsOpen)
            .Include(n => n.Author)
            .ToListAsync();

    [HttpGet]
    public async Task<IActionResult> GetNote(Guid id, Guid userId)
    {
        var note = await _context.Notes
            .Where(n => n.Id.Equals(id))
            .Include(n => n.NoteTags)
            .ThenInclude(n => n.Tag)
            .Include(n => n.Author)
            .Include(n => n.Files)
            .FirstOrDefaultAsync();
        if (note == null) return NotFound();

        var access = await _context.NoteAccesses
            .Where(a => a.NoteId == id)
            .ToListAsync();
        if (!note.IsOpen && !access.Any(u => u.UserId == userId)) return Forbid();

        return Ok(note);
    }

    [HttpGet]
    public async Task<IEnumerable<Note>> GetAllNotesByUser(Guid userId) =>
        await _context.Notes
            .Where(n => n.Author.Id == userId && n.IsOpen)
            .ToListAsync();

    [HttpGet]
    public async Task<IEnumerable<Note>> GetMyNotes(Guid userId) =>
        await _context.Notes
            .Where(n => n.Author.Id == userId)
            .ToListAsync();

    [HttpPost]
    public async Task<IActionResult> CreateNote([FromForm]SaveNoteRequest request)
    {
        Guid id = Guid.NewGuid();
        var newPath = $"FileStorage/Notes/{id}";
        var dirInfo = new DirectoryInfo(newPath);
        if (!dirInfo.Exists) dirInfo.Create();

        var note = new Note();
        note.Id = id;
        note.Author = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.AuthorId);
        note.Title = request.Title;
        note.Description = request.Description;
        note.Text = request.Text;
        note.IsOpen = request.IsOpen;
        note.NoteTags = [];
        var currentTags = await _context.Tags.Where(t => request.TagsNames.Contains(t.Name)).ToListAsync();
        foreach (var tag in currentTags)
        {
            note.NoteTags.Add(new NoteTag
            {
                Note = note,
                NoteId = note.Id,
                Tag = tag,
                TagId = tag.Id
            });
        }

        note.Files = [];
        if (request.Files != null)
        {
            foreach (var file in request.Files)
            {
                var path = $"FileStorage/Notes/{note.Id}/{file.FileName}";
                using (var fs = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(fs);
                }

                note.Files.Add(new Models.File {
                    Id = Guid.NewGuid(),
                    CustomFileName = file.FileName
                });
            }
        }

        await _context.Notes.AddAsync(note);
        await _context.SaveChangesAsync();
        return Created();
    }

    [HttpGet]
    public async Task<IActionResult> GetEditNote(Guid noteId, Guid userId)
    {
        var note = await _context.Notes
            .Include(n => n.Author)
            .Include(n => n.Files)
            .Include(n => n.NoteAccess)
            .ThenInclude(n => n.User)
            .Include(n => n.NoteTags)
            .ThenInclude(n => n.Tag)
            .FirstOrDefaultAsync(n => n.Id == noteId);
        var userAccess = await _context.NoteAccesses.Where(n => n.NoteId == noteId && n.UserId == userId).FirstOrDefaultAsync();

        if (note == null) return NotFound();
        if (userAccess != null)
            if (!userAccess.CanEdit) return BadRequest();
        else
            if (userId != note.Author.Id) return BadRequest();

        return Ok(note);
    }

    [HttpPatch]
    public async Task<IActionResult> EditNote(Guid id, EditNoteRequest request)
    {
        var note = await _context.Notes
            .Include(n => n.NoteTags)
            .ThenInclude(n => n.Tag)
            .Include(n => n.Files)
            .FirstOrDefaultAsync(n => n.Id == id);
        if (note == null) return NotFound();

        note.Title = request.Title;
        note.Description = request.Description;
        note.Text = request.Text;
        note.IsOpen = request.IsOpen;
        var currentTags = await _context.Tags.Where(t => request.TagsNames.Contains(t.Name)).ToListAsync();
        foreach (var tag in currentTags)
        {
            if (note.NoteTags.Any(t => t.Tag == tag)) continue;

            note.NoteTags.Add(new NoteTag
            {
                Note = note,
                NoteId = note.Id,
                Tag = tag,
                TagId = tag.Id
            });
        }

        if (request.Files != null)
        {
            foreach (var file in request.Files)
            {
                if (note.Files.Any(f => f.CustomFileName == file.FileName)) continue;

                var path = $"FileNotes/Notes/{note.Id}/{file.FileName}";
                using (var fs = new FileStream(path, FileMode.Create))
                {
                    await file.CopyToAsync(fs);
                }

                note.Files.Add(new Models.File {
                    Id = Guid.NewGuid(),
                    CustomFileName = file.FileName 
                });
            }
        }

        _context.Notes.Update(note);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteNote(Guid id)
    {
        var note = await _context.Notes
            .Include(n => n.NoteTags)
            .Include(n => n.NoteAccess)
            .Include(n => n.Files)
            .FirstOrDefaultAsync(n => n.Id == id);
        if (note == null) return NotFound();

        var path = $"FileStorage/Notes/{id}";
        var dirInfo = new DirectoryInfo(path);
        if (dirInfo.Exists) dirInfo.Delete(true);

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync();
        return Ok();
    }

    [HttpGet]
    public async Task<IEnumerable<Note>> OpenNotesByTags([FromQuery]List<string> tagNames)
    {
        var openNotes = await _context.Notes
            .Where(n => n.IsOpen)
            .Include(n => n.NoteTags)
            .ThenInclude(nt => nt.Tag)
            .ToListAsync();

        foreach (var tagName in tagNames)
            openNotes = openNotes.Where(n => n.NoteTags.Any(nt => nt.Tag.Name == tagName)).ToList();

        return openNotes;
    }

    [HttpGet]
    public async Task<IEnumerable<Note>> UserNotesByTags(Guid userId, [FromQuery]List<string> tagNames)
    {
        var userNotes = await _context.Notes
            .Where(n => n.Author.Id == userId && n.IsOpen)
            .Include(n => n.NoteTags)
            .ThenInclude(nt => nt.Tag)
            .ToListAsync();

        foreach (var tagName in tagNames)
            userNotes = userNotes.Where(n => n.NoteTags.Any(nt => nt.Tag.Name == tagName)).ToList();
        
        return userNotes;
    }
}