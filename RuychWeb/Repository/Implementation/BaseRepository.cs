﻿using Microsoft.EntityFrameworkCore;
using RuychWeb.Repository.Abstract;
using System.Linq.Expressions;

namespace RuychWeb.Repository.Implementation
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        private readonly DataContext ctx;
        public BaseRepository(DataContext ctx)
        {
            this.ctx = ctx;
        }
        public bool Create(T entity)
        {
            try
            {
                ctx.Set<T>().Add(entity);
                ctx.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool Delete(T entity)
        {
            try
            {
                ctx.Set<T>().Remove(entity);
                ctx.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool Update(T entity)
        {
            try
            {
                ctx.Set<T>().Update(entity);
                ctx.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public T? FindById(int id)
        {
            return ctx.Set<T>().Find(id);
        }

        public IQueryable<T> GetAll()
        {
            return ctx.Set<T>().AsNoTracking();
        }

        public IQueryable<T> GetAll(Expression<Func<T, bool>> expression)
        {
            return ctx.Set<T>().Where(expression).AsNoTracking();

        }


    }

}
